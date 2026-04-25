using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;

public class WheelchairFinale : MonoBehaviour
{
    [Header("=== MONSTER ===")]
    [SerializeField] private GameObject monster;
    [SerializeField] private Animator monsterAnimator;
    [SerializeField] private Transform monsterHeadBone;

    [Header("=== ANIMATOR STATE NAMES ===")]
    [SerializeField] private string chaseStateName = "Chase";
    [SerializeField] private string attackStateName = "Attack";
    [SerializeField] private string idleStateName = "Idle";

    [Header("=== CROSS-SCENE REFERENCES (names only) ===")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private string postFXGameObjectName = "HorrorPostFX";
    [SerializeField] private string redVignetteName = "RedVignette";
    [SerializeField] private string fadeImageName = "FadeImage";

    [Header("=== AUDIO ===")]
    [SerializeField] private AudioSource gnarlyBehindSound;
    [SerializeField] private AudioSource heartbeatAudio;
    [SerializeField] private AudioSource outdoorAmbient;
    [SerializeField] private AudioSource lowRumble;
    [SerializeField] private AudioSource shadowReveal;
    [SerializeField] private AudioSource chaseAudio;
    [SerializeField] private AudioSource attackBoom;
    [SerializeField] private AudioSource attackScreech;

    [Header("=== SEQUENCE TIMING ===")]
    [SerializeField] private float preTrapDelay = 0.2f;
    [SerializeField] private float gnarlySoundHoldDuration = 1.2f;
    [SerializeField] private float heartbeatOnlyDuration = 0.8f;
    [SerializeField] private float fadeOutDuration = 0.5f;
    [SerializeField] private float blackHoldDuration = 0.4f;
    [SerializeField] private float fadeInDuration = 0.25f;
    [Tooltip("How long the shadow appears on the floor before monster reveals.")]
    [SerializeField] private float shadowOnFloorDuration = 1.2f;
    [SerializeField] private float startChaseSpeed = 9f;
    [SerializeField] private float maxChaseSpeed = 16f;
    [SerializeField] private float chaseAcceleration = 8f;
    [SerializeField] private float attackDistance = 2.8f;
    [SerializeField] private float maxChaseTime = 3.5f;
    [SerializeField] private float silenceBeforeAttack = 0.15f;

    [Header("=== MONSTER SPAWN POSITIONING ===")]
    [Tooltip("How far from the player to spawn the monster (in the direction they're looking).")]
    [SerializeField] private float spawnDistanceFromPlayer = 8f;

    [Header("=== SHADOW ON FLOOR ===")]
    [Tooltip("Light placed above the monster to cast its shadow on the floor.")]
    [SerializeField] private Light shadowCastingLight;
    [Tooltip("Height of the shadow casting light above the monster.")]
    [SerializeField] private float shadowLightHeight = 6f;

    [Header("=== GROUND SNAPPING ===")]
    [SerializeField] private LayerMask groundMask = ~0;
    [SerializeField] private float groundRayStartHeight = 2f;
    [SerializeField] private float groundRayDistance = 10f;

    [Header("=== ATTACK POSITION (seated player) ===")]
    [SerializeField] private float attackStopDistance = 1.8f;
    [SerializeField] private float finalHeightOffset = 0f;
    [SerializeField] private bool useManualHeight = true;
    [SerializeField] private float manualHeightOverride = -0.4f;

    [Header("=== LIGHTING ===")]
    [SerializeField] private string[] scenesToDarken = { "final_jumpscare", "Second Area" };
    [SerializeField] private Light monsterSpotlight;
    [SerializeField] private Color chaseAmbientColor = new Color(0.2f, 0.02f, 0.02f);
    [SerializeField] private float chaseAmbientIntensity = 0.3f;
    [SerializeField] private float redRampDuration = 0.6f;

    [Header("=== FOG ===")]
    [SerializeField] private bool useFogDuringChase = true;
    [SerializeField] private Color chaseFogColor = new Color(0.15f, 0.02f, 0.02f);
    [SerializeField] private float chaseFogDensity = 0.25f;
    [SerializeField] private FogMode chaseFogMode = FogMode.Exponential;

    [Header("=== IMPACT EFFECTS ===")]
    [SerializeField] private float redFlashDuration = 0.3f;
    [SerializeField] private float holdOnBlackDuration = 3f;

    [Header("=== CAMERA SHAKE ===")]
    [SerializeField] private float cameraShakeChaseIntensity = 0.015f;
    [SerializeField] private float cameraShakeAttackIntensity = 0.08f;

    [Header("=== CALIBRATION ===")]
    [SerializeField] private bool enableCalibrationMode = true;

    // Runtime
    private Transform playerCamera, playerRig;
    private ContinuousMoveProvider moveProvider;
    private ContinuousTurnProvider turnProvider;
    private SnapTurnProvider snapTurnProvider;
    private Volume horrorPostFX;
    private Image redVignetteImage;
    private Image fadeImage;

    // State
    private bool hasTriggered = false;
    private bool isShakingCamera = false;
    private Vector3 rigShakeBasePos;
    private Vector3 plannedSpawnPosition; // where the monster will appear

    // Cached lighting
    private List<Light> disabledLights = new List<Light>();
    private AmbientMode cachedAmbientMode;
    private Color cachedAmbientLight;
    private float cachedAmbientIntensity, cachedReflectionIntensity;
    private bool cachedFogEnabled;
    private Color cachedFogColor;
    private float cachedFogDensity;

    private void Awake()
    {
        if (monster != null) monster.SetActive(false);
        if (monsterSpotlight != null) monsterSpotlight.enabled = false;
        if (shadowCastingLight != null) shadowCastingLight.enabled = false;
    }

    private void Update()
    {
        if (!enableCalibrationMode) return;
        if (Input.GetKeyDown(KeyCode.C)) TestAttackPosition();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered) return;
        if (!other.CompareTag(playerTag)) return;
        hasTriggered = true;
        StartCoroutine(PlayScare());
    }

    private void FindRuntimeReferences()
    {
        if (Camera.main != null) playerCamera = Camera.main.transform;

        GameObject p = GameObject.FindGameObjectWithTag(playerTag);
        if (p != null)
        {
            playerRig = p.transform;
            moveProvider = p.GetComponentInChildren<ContinuousMoveProvider>();
            turnProvider = p.GetComponentInChildren<ContinuousTurnProvider>();
            snapTurnProvider = p.GetComponentInChildren<SnapTurnProvider>();
        }

        GameObject postFX = GameObject.Find(postFXGameObjectName);
        if (postFX != null) horrorPostFX = postFX.GetComponent<Volume>();

        GameObject vignette = GameObject.Find(redVignetteName);
        if (vignette != null) redVignetteImage = vignette.GetComponent<Image>();

        GameObject fade = GameObject.Find(fadeImageName);
        if (fade != null) fadeImage = fade.GetComponent<Image>();
    }

    private IEnumerator PlayScare()
    {
        FindRuntimeReferences();
        EnsureFadeImageExists();

        if (redVignetteImage != null) redVignetteImage.color = new Color(0.9f, 0, 0, 0);
        if (horrorPostFX != null) horrorPostFX.weight = 0f;

        if (monster == null || monsterAnimator == null || playerCamera == null || playerRig == null)
        {
            Debug.LogError("[WheelchairFinale] missing critical refs - aborting");
            yield break;
        }

        CacheLightingState();

        // === ACT 1: PRE-TRAP DELAY ===
        yield return new WaitForSeconds(preTrapDelay);

        // === ACT 2: TRAP - lock controls + gnarly sound ===
        Debug.LogError("[Act 2] Disable controls + gnarly sound");
        DisableControllers();

        // Gnarly sound plays from in front of where player is looking
        // (so they know something's there but can't tell exactly where)
        PlayGnarlyInFrontOfPlayer();
        StartCoroutine(DuckAudioSource(outdoorAmbient, 0.15f, 1.5f));

        yield return new WaitForSeconds(gnarlySoundHoldDuration);

        // === ACT 3: HEARTBEAT BUILDS ===
        Debug.LogError("[Act 3] Heartbeat + dread");
        if (heartbeatAudio != null)
        {
            heartbeatAudio.pitch = 0.9f;
            heartbeatAudio.Play();
        }
        if (lowRumble != null)
        {
            lowRumble.volume = 0.3f;
            lowRumble.Play();
            StartCoroutine(RampAudioVolume(lowRumble, 0.8f, heartbeatOnlyDuration));
        }
        if (outdoorAmbient != null) outdoorAmbient.Stop();

        yield return new WaitForSeconds(heartbeatOnlyDuration);

        // === ACT 4: FADE TO BLACK ===
        Debug.LogError("[Act 4] Fade to black");
        StartCoroutine(RampPostFX(1f, 1f));
        yield return StartCoroutine(FadeColor(new Color(0, 0, 0, 0), new Color(0, 0, 0, 1), fadeOutDuration));

        // === ACT 5: IN DARKNESS - position monster wherever the player is looking ===
        Debug.LogError("[Act 5] In darkness - place monster where player is looking");
        DarkenWorldAndGoRed();
        if (useFogDuringChase) ApplyHorrorFog();

        // Place monster in the direction the player is currently looking
        SpawnMonsterInPlayersView();

        monster.SetActive(true);
        monsterAnimator.applyRootMotion = false;
        monsterAnimator.Play(idleStateName, 0, 0f);

        // Set up the shadow-casting light ABOVE the monster
        SetupShadowCastingLight();

        if (shadowReveal != null) shadowReveal.Play();
        if (heartbeatAudio != null) StartCoroutine(RampPitch(heartbeatAudio, 1.3f, blackHoldDuration));

        yield return new WaitForSeconds(blackHoldDuration);

        // === ACT 6: FADE IN - shadow visible on floor first ===
        Debug.LogError("[Act 6] Fade in - shadow appears on floor");
        yield return StartCoroutine(FadeColor(new Color(0, 0, 0, 1), new Color(0, 0, 0, 0), fadeInDuration));

        StartCoroutine(PulseRedVignette());

        // Player sees the shadow on the floor — but the monster is far away
        // The shadow grows as the monster gets closer (light is high above casting it)
        Debug.LogError("[Act 6.5] Shadow on floor - building dread");
        yield return new WaitForSeconds(shadowOnFloorDuration);

        // === ACT 7: CHASE BEGINS ===
        Debug.LogError("[Act 7] Charge");
        if (monsterSpotlight != null) monsterSpotlight.enabled = true;
        monsterAnimator.Play(chaseStateName, 0, 0f);
        if (shadowReveal != null && shadowReveal.isPlaying) shadowReveal.Stop();
        if (chaseAudio != null) chaseAudio.Play();
        if (heartbeatAudio != null) heartbeatAudio.pitch = 1.5f;

        isShakingCamera = true;
        rigShakeBasePos = playerRig.localPosition;
        StartCoroutine(CameraShakeLoop(cameraShakeChaseIntensity));

        float currentSpeed = startChaseSpeed;
        float timer = 0f;

        while (timer < maxChaseTime)
        {
            currentSpeed = Mathf.Min(currentSpeed + chaseAcceleration * Time.deltaTime, maxChaseSpeed);

            Vector3 target = new Vector3(playerCamera.position.x,
                                         monster.transform.position.y,
                                         playerCamera.position.z);

            float horizontalDist = Vector2.Distance(
                new Vector2(monster.transform.position.x, monster.transform.position.z),
                new Vector2(playerCamera.position.x, playerCamera.position.z));

            if (horizontalDist <= attackDistance) break;

            Vector3 nextPos = Vector3.MoveTowards(monster.transform.position, target,
                                                   currentSpeed * Time.deltaTime);

            if (Physics.Raycast(nextPos + Vector3.up * groundRayStartHeight,
                                Vector3.down, out RaycastHit hit,
                                groundRayDistance, groundMask,
                                QueryTriggerInteraction.Ignore))
            {
                nextPos.y = hit.point.y;
            }

            monster.transform.position = nextPos;
            FacePlayerHorizontal();

            // Keep shadow light above the monster as it moves
            if (shadowCastingLight != null)
            {
                shadowCastingLight.transform.position = monster.transform.position + Vector3.up * shadowLightHeight;
            }

            timer += Time.deltaTime;
            yield return null;
        }

        // === ACT 8: ATTACK POSITION ===
        PositionMonsterForAttack();

        if (chaseAudio != null && chaseAudio.isPlaying) chaseAudio.Stop();
        if (heartbeatAudio != null && heartbeatAudio.isPlaying) heartbeatAudio.Stop();
        if (lowRumble != null && lowRumble.isPlaying) lowRumble.Stop();

        // === ACT 9: SILENCE ===
        yield return new WaitForSeconds(silenceBeforeAttack);

        // === ACT 10: ATTACK ===
        Debug.LogError("[Act 10] ATTACK");
        monsterAnimator.Play(attackStateName, 0, 0f);
        if (attackBoom != null) attackBoom.Play();
        if (attackScreech != null) attackScreech.Play();

        StartCoroutine(CameraShakeBurst(cameraShakeAttackIntensity, redFlashDuration));
        yield return StartCoroutine(RedFlashAndFillVision());

        // === ACT 11: HOLD ===
        isShakingCamera = false;
        yield return new WaitForSeconds(holdOnBlackDuration);

        OnFinaleComplete();
    }

    // ============================================================
    // SPAWN MONSTER IN PLAYER'S VIEW
    // ============================================================
    private void SpawnMonsterInPlayersView()
    {
        // Get the direction the player is currently looking (horizontal only)
        Vector3 lookDir = playerCamera.forward;
        lookDir.y = 0;
        lookDir.Normalize();

        // Place monster in front of the player at spawn distance
        Vector3 spawnPos = playerCamera.position + lookDir * spawnDistanceFromPlayer;

        // Snap to ground
        if (Physics.Raycast(spawnPos + Vector3.up * groundRayStartHeight,
                            Vector3.down, out RaycastHit hit,
                            groundRayDistance * 2f, groundMask,
                            QueryTriggerInteraction.Ignore))
        {
            spawnPos.y = hit.point.y;
        }
        else
        {
            spawnPos.y = playerCamera.position.y - 1.5f;
        }

        monster.transform.position = spawnPos;
        plannedSpawnPosition = spawnPos;
        FacePlayerHorizontal();

        Debug.LogError("[SPAWN] Monster placed at: " + spawnPos +
                       " | " + spawnDistanceFromPlayer + "m in front of player");
    }

    // ============================================================
    // SHADOW CASTING LIGHT
    // ============================================================
    private void SetupShadowCastingLight()
    {
        if (shadowCastingLight == null) return;

        // Position the light directly above the monster
        shadowCastingLight.transform.position = monster.transform.position + Vector3.up * shadowLightHeight;
        shadowCastingLight.transform.rotation = Quaternion.Euler(90f, 0f, 0f); // Point straight down

        // Configure for shadow casting
        shadowCastingLight.type = LightType.Spot;
        shadowCastingLight.spotAngle = 60f;
        shadowCastingLight.range = shadowLightHeight + 5f;
        shadowCastingLight.shadows = LightShadows.Hard;
        shadowCastingLight.intensity = 4f;
        shadowCastingLight.color = new Color(1f, 0.3f, 0.3f); // tint red
        shadowCastingLight.enabled = true;

        Debug.LogError("[SHADOW] Light positioned " + shadowLightHeight + "m above monster");
    }

    // ============================================================
    // GNARLY SOUND - in front of player
    // ============================================================
    private void PlayGnarlyInFrontOfPlayer()
    {
        if (gnarlyBehindSound == null) return;

        // Play sound in front of where player is looking
        Vector3 forwardDir = playerCamera.forward;
        forwardDir.y = 0;
        forwardDir.Normalize();

        gnarlyBehindSound.transform.position = playerCamera.position + forwardDir * 5f;
        gnarlyBehindSound.spatialBlend = 1f;
        gnarlyBehindSound.Play();
    }

    // ============================================================
    // ATTACK POSITIONING
    // ============================================================
    private void PositionMonsterForAttack()
    {
        Vector3 dirToPlayer = (playerCamera.position - monster.transform.position);
        dirToPlayer.y = 0;
        dirToPlayer.Normalize();

        Vector3 targetPos = playerCamera.position - dirToPlayer * attackStopDistance;
        monster.transform.position = targetPos;
        FacePlayerHorizontal();

        if (useManualHeight)
        {
            Vector3 adjusted = monster.transform.position;
            adjusted.y = playerCamera.position.y + manualHeightOverride;
            monster.transform.position = adjusted;
            return;
        }

        if (monsterHeadBone != null)
        {
            monsterAnimator.Update(0f);
            float headYDelta = monsterHeadBone.position.y - monster.transform.position.y;
            float correctY = playerCamera.position.y - headYDelta + finalHeightOffset;

            Vector3 adjusted = monster.transform.position;
            adjusted.y = correctY;
            monster.transform.position = adjusted;
        }
    }

    private void TestAttackPosition()
    {
        FindRuntimeReferences();

        if (monster == null || playerCamera == null) return;

        monster.SetActive(true);
        monsterAnimator.applyRootMotion = false;
        monsterAnimator.Play(idleStateName, 0, 0f);

        SpawnMonsterInPlayersView();
        PositionMonsterForAttack();

        Debug.LogError("[CALIBRATE] Cam Y: " + playerCamera.position.y +
                       " | Monster Y: " + monster.transform.position.y);
    }

    // ============================================================
    // LIGHTING
    // ============================================================
    private void CacheLightingState()
    {
        cachedAmbientMode = RenderSettings.ambientMode;
        cachedAmbientLight = RenderSettings.ambientLight;
        cachedAmbientIntensity = RenderSettings.ambientIntensity;
        cachedReflectionIntensity = RenderSettings.reflectionIntensity;
        cachedFogEnabled = RenderSettings.fog;
        cachedFogColor = RenderSettings.fogColor;
        cachedFogDensity = RenderSettings.fogDensity;
    }

    private void DarkenWorldAndGoRed()
    {
        disabledLights.Clear();

        foreach (string sceneName in scenesToDarken)
        {
            Scene scene = SceneManager.GetSceneByName(sceneName);
            if (!scene.IsValid() || !scene.isLoaded) continue;

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Light[] lights = root.GetComponentsInChildren<Light>(true);
                foreach (Light l in lights)
                {
                    if (l == monsterSpotlight) continue;
                    if (l == shadowCastingLight) continue;
                    if (!l.enabled) continue;
                    l.enabled = false;
                    disabledLights.Add(l);
                }
            }
        }

        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.02f, 0.01f, 0.01f);
        RenderSettings.ambientIntensity = 0.08f;
        RenderSettings.reflectionIntensity = 0.1f;

        StartCoroutine(RampToRedAmbient());
    }

    private IEnumerator RampToRedAmbient()
    {
        yield return new WaitForSeconds(shadowOnFloorDuration + fadeInDuration);

        Color startColor = RenderSettings.ambientLight;
        float startIntensity = RenderSettings.ambientIntensity;
        float elapsed = 0f;

        while (elapsed < redRampDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / redRampDuration;
            RenderSettings.ambientLight = Color.Lerp(startColor, chaseAmbientColor, t);
            RenderSettings.ambientIntensity = Mathf.Lerp(startIntensity, chaseAmbientIntensity, t);
            yield return null;
        }
    }

    private void ApplyHorrorFog()
    {
        RenderSettings.fog = true;
        RenderSettings.fogColor = chaseFogColor;
        RenderSettings.fogMode = chaseFogMode;
        RenderSettings.fogDensity = chaseFogDensity;
    }

    // ============================================================
    // POST PROCESSING
    // ============================================================
    private IEnumerator RampPostFX(float targetWeight, float duration)
    {
        if (horrorPostFX == null) yield break;
        float start = horrorPostFX.weight;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            horrorPostFX.weight = Mathf.Lerp(start, targetWeight, elapsed / duration);
            yield return null;
        }
        horrorPostFX.weight = targetWeight;
    }

    // ============================================================
    // AUDIO HELPERS
    // ============================================================
    private IEnumerator DuckAudioSource(AudioSource src, float targetVolume, float duration)
    {
        if (src == null) yield break;
        float startVol = src.volume;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            src.volume = Mathf.Lerp(startVol, targetVolume, elapsed / duration);
            yield return null;
        }
    }

    private IEnumerator RampAudioVolume(AudioSource src, float targetVolume, float duration)
    {
        if (src == null) yield break;
        float startVol = src.volume;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            src.volume = Mathf.Lerp(startVol, targetVolume, elapsed / duration);
            yield return null;
        }
    }

    private IEnumerator RampPitch(AudioSource src, float targetPitch, float duration)
    {
        if (src == null) yield break;
        float startPitch = src.pitch;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            src.pitch = Mathf.Lerp(startPitch, targetPitch, elapsed / duration);
            yield return null;
        }
    }

    // ============================================================
    // VISUAL EFFECTS
    // ============================================================
    private IEnumerator PulseRedVignette()
    {
        if (redVignetteImage == null) yield break;
        Color off = new Color(0.9f, 0f, 0f, 0f);
        Color on = new Color(0.9f, 0f, 0f, 0.5f);
        float t = 0f;
        while (isShakingCamera || t < 2f)
        {
            t += Time.deltaTime;
            float pulse = (Mathf.Sin(t * 6f) + 1f) * 0.5f;
            redVignetteImage.color = Color.Lerp(off, on, pulse);
            yield return null;
        }
        redVignetteImage.color = off;
    }

    // ============================================================
    // CAMERA SHAKE
    // ============================================================
    private IEnumerator CameraShakeLoop(float intensity)
    {
        while (isShakingCamera)
        {
            Vector3 shake = Random.insideUnitSphere * intensity;
            shake.y *= 0.3f;
            playerRig.localPosition = rigShakeBasePos + shake;
            yield return new WaitForSeconds(0.03f);
        }
        playerRig.localPosition = rigShakeBasePos;
    }

    private IEnumerator CameraShakeBurst(float intensity, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            Vector3 shake = Random.insideUnitSphere * intensity;
            shake.y *= 0.3f;
            playerRig.localPosition = rigShakeBasePos + shake;
            yield return null;
        }
        playerRig.localPosition = rigShakeBasePos;
    }

    // ============================================================
    // GENERAL HELPERS
    // ============================================================
    private void DisableControllers()
    {
        if (moveProvider != null) moveProvider.enabled = false;
        if (turnProvider != null) turnProvider.enabled = false;
        if (snapTurnProvider != null) snapTurnProvider.enabled = false;
    }

    private void FacePlayerHorizontal()
    {
        Vector3 lookTarget = new Vector3(playerCamera.position.x,
                                         monster.transform.position.y,
                                         playerCamera.position.z);
        monster.transform.LookAt(lookTarget);
    }

    private IEnumerator FadeColor(Color startColor, Color endColor, float duration)
    {
        if (fadeImage == null) yield break;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            fadeImage.color = Color.Lerp(startColor, endColor, elapsed / duration);
            yield return null;
        }
        fadeImage.color = endColor;
    }

    private IEnumerator RedFlashAndFillVision()
    {
        if (fadeImage == null) yield break;

        Color red = new Color(0.9f, 0f, 0f, 0.8f);
        Color black = new Color(0, 0, 0, 1);
        fadeImage.color = red;

        Vector3 monsterStart = monster.transform.position;
        Vector3 cameraPos = playerCamera.position;
        float elapsed = 0f;

        while (elapsed < redFlashDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / redFlashDuration;
            monster.transform.position = Vector3.Lerp(monsterStart, cameraPos, t);
            monster.transform.LookAt(playerCamera.position);
            fadeImage.color = Color.Lerp(red, black, t);
            yield return null;
        }
        fadeImage.color = black;
    }

    private void EnsureFadeImageExists()
    {
        if (fadeImage != null) return;

        GameObject canvasGO = new GameObject("RuntimeFadeCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32767;
        canvasGO.AddComponent<CanvasScaler>();

        GameObject imgGO = new GameObject("FadeImage");
        imgGO.transform.SetParent(canvasGO.transform, false);
        fadeImage = imgGO.AddComponent<Image>();
        fadeImage.color = new Color(0, 0, 0, 0);

        RectTransform rt = fadeImage.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private void OnFinaleComplete()
    {
        Debug.LogError("[WheelchairFinale] finale complete");
    }
}