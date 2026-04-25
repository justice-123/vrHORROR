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
    [Tooltip("Optional: drag head bone for attack height alignment. Not required if using manual stop distance.")]
    [SerializeField] private Transform monsterHeadBone;

    [Header("=== ANIMATOR STATE NAMES ===")]
    [SerializeField] private string chaseStateName = "Chase";
    [SerializeField] private string attackStateName = "Attack";
    [SerializeField] private string idleStateName = "Idle";

    [Header("=== CROSS-SCENE REFERENCES (names only, auto-found) ===")]
    [SerializeField] private string doorMarkerName = "DoorMarker";
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private string postFXGameObjectName = "HorrorPostFX";
    [SerializeField] private string redVignetteName = "RedVignette";
    [SerializeField] private string fadeImageName = "FadeImage";

    [Header("=== AUDIO ===")]
    [Tooltip("Gnarly sound that plays BEHIND the player when trapped. 3D spatial.")]
    [SerializeField] private AudioSource gnarlyBehindSound;
    [Tooltip("Slow heartbeat that starts after gnarly sound.")]
    [SerializeField] private AudioSource heartbeatAudio;
    [Tooltip("Ambient outdoor sound - gets ducked/stopped.")]
    [SerializeField] private AudioSource outdoorAmbient;
    [Tooltip("Low rumble that builds as dread grows.")]
    [SerializeField] private AudioSource lowRumble;
    [Tooltip("Monster growl/breath during shadow reveal.")]
    [SerializeField] private AudioSource shadowReveal;
    [Tooltip("Chase audio - skittering/footsteps.")]
    [SerializeField] private AudioSource chaseAudio;
    [Tooltip("Big attack boom.")]
    [SerializeField] private AudioSource attackBoom;
    [Tooltip("Attack screech.")]
    [SerializeField] private AudioSource attackScreech;

    [Header("=== SEQUENCE TIMING ===")]
    [SerializeField] private float preTrapDelay = 0.2f;
    [SerializeField] private float gnarlySoundHoldDuration = 1.2f;
    [SerializeField] private float heartbeatOnlyDuration = 1f;
    [SerializeField] private float fadeOutDuration = 1.2f;
    [SerializeField] private float blackHoldDuration = 1.5f;
    [SerializeField] private float fadeInDuration = 0.6f;
    [SerializeField] private float shadowRevealDuration = 2f;
    [SerializeField] private float startChaseSpeed = 9f;
    [SerializeField] private float maxChaseSpeed = 16f;
    [SerializeField] private float chaseAcceleration = 8f;
    [Tooltip("Monster stops chasing at this distance - bigger value = stops further away.")]
    [SerializeField] private float attackDistance = 2.8f;
    [SerializeField] private float maxChaseTime = 3.5f;
    [SerializeField] private float silenceBeforeAttack = 0.15f;

    [Header("=== GROUND SNAPPING ===")]
    [SerializeField] private LayerMask groundMask = ~0;
    [SerializeField] private float groundRayStartHeight = 2f;
    [SerializeField] private float groundRayDistance = 10f;

    [Header("=== ATTACK POSITION (seated player) ===")]
    [Tooltip("How far in front of player the monster ends up. ~1.8 keeps head naturally at eye level.")]
    [SerializeField] private float attackStopDistance = 1.8f;
    [Tooltip("Vertical offset from camera Y. 0 = same height as camera; negative = lower.")]
    [SerializeField] private float finalHeightOffset = 0f;
    [Tooltip("Tick this to override everything with a manual Y position relative to camera.")]
    [SerializeField] private bool useManualHeight = false;
    [SerializeField] private float manualHeightOverride = -0.3f;

    [Header("=== LIGHTING: BRIGHT → RED HELLSCAPE ===")]
    [SerializeField] private string[] scenesToDarken = { "final_jumpscare", "Second Area" };
    [Tooltip("Dim light BEHIND the monster for silhouette effect during reveal.")]
    [SerializeField] private Light shadowBacklight;
    [Tooltip("Red spotlight on monster during chase.")]
    [SerializeField] private Light monsterSpotlight;
    [SerializeField] private Color chaseAmbientColor = new Color(0.2f, 0.02f, 0.02f);
    [SerializeField] private float chaseAmbientIntensity = 0.3f;
    [SerializeField] private float redRampDuration = 0.6f;

    [Header("=== FOG (atmosphere) ===")]
    [SerializeField] private bool useFogDuringChase = true;
    [SerializeField] private Color chaseFogColor = new Color(0.15f, 0.02f, 0.02f);
    [Tooltip("Higher = thicker fog. 0.25 = thick horror fog. 0.08 = barely visible.")]
    [SerializeField] private float chaseFogDensity = 0.25f;
    [SerializeField] private FogMode chaseFogMode = FogMode.Exponential;

    [Header("=== IMPACT EFFECTS ===")]
    [SerializeField] private float redFlashDuration = 0.3f;
    [SerializeField] private float holdOnBlackDuration = 3f;

    [Header("=== CAMERA SHAKE ===")]
    [SerializeField] private float cameraShakeChaseIntensity = 0.015f;
    [SerializeField] private float cameraShakeAttackIntensity = 0.08f;

    [Header("=== CALIBRATION (for testing) ===")]
    [Tooltip("Press C during play to test monster attack position.")]
    [SerializeField] private bool enableCalibrationMode = true;

    // === Runtime references (auto-found) ===
    private Transform playerCamera, playerRig, doorMarker;
    private ContinuousMoveProvider moveProvider;
    private ContinuousTurnProvider turnProvider;
    private SnapTurnProvider snapTurnProvider;
    private Volume horrorPostFX;
    private Image redVignetteImage;
    private Image fadeImage;

    // === State ===
    private bool hasTriggered = false;
    private bool isShakingCamera = false;
    private Vector3 rigShakeBasePos;

    // === Cached lighting ===
    private List<Light> disabledLights = new List<Light>();
    private AmbientMode cachedAmbientMode;
    private Color cachedAmbientLight;
    private float cachedAmbientIntensity, cachedReflectionIntensity;
    private bool cachedFogEnabled;
    private Color cachedFogColor;
    private float cachedFogDensity;

    private void Awake()
    {
        Debug.LogError("[WheelchairFinale] Awake");
        if (monster != null) monster.SetActive(false);
        if (monsterSpotlight != null) monsterSpotlight.enabled = false;
        if (shadowBacklight != null) shadowBacklight.enabled = false;
    }

    private void Update()
    {
        if (!enableCalibrationMode) return;
        if (Input.GetKeyDown(KeyCode.C))
        {
            TestAttackPosition();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered) return;
        if (!other.CompareTag(playerTag)) return;
        hasTriggered = true;
        StartCoroutine(PlayScare());
    }

    // ============================================================
    // FIND CROSS-SCENE REFERENCES BY NAME
    // ============================================================
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

        GameObject m = GameObject.Find(doorMarkerName);
        if (m != null) doorMarker = m.transform;

        GameObject postFX = GameObject.Find(postFXGameObjectName);
        if (postFX != null) horrorPostFX = postFX.GetComponent<Volume>();

        GameObject vignette = GameObject.Find(redVignetteName);
        if (vignette != null) redVignetteImage = vignette.GetComponent<Image>();

        GameObject fade = GameObject.Find(fadeImageName);
        if (fade != null) fadeImage = fade.GetComponent<Image>();

        Debug.LogError("[REFS] cam=" + (playerCamera != null) +
                       " rig=" + (playerRig != null) +
                       " door=" + (doorMarker != null) +
                       " postFX=" + (horrorPostFX != null) +
                       " vignette=" + (redVignetteImage != null) +
                       " fade=" + (fadeImage != null));
    }

    // ============================================================
    // MAIN SCARE SEQUENCE
    // ============================================================
    private IEnumerator PlayScare()
    {
        FindRuntimeReferences();
        EnsureFadeImageExists();

        // Reset UI/PostFX after finding them
        if (redVignetteImage != null) redVignetteImage.color = new Color(0.9f, 0, 0, 0);
        if (horrorPostFX != null) horrorPostFX.weight = 0f;

        if (monster == null || monsterAnimator == null || playerCamera == null ||
            doorMarker == null || playerRig == null)
        {
            Debug.LogError("[WheelchairFinale] missing critical refs - aborting");
            yield break;
        }

        CacheLightingState();

        // === ACT 1: PRE-TRAP DELAY ===
        Debug.LogError("[Act 1] Pre-trap");
        yield return new WaitForSeconds(preTrapDelay);

        // === ACT 2: TRAP + ROTATE + GNARLY SOUND FROM THE RIGHT ===
        Debug.LogError("[Act 2] Disable controls, rotate player, gnarly sound to the right");
        DisableControllers();

        // Rotate NOW so the sound comes from the correct direction
        RotatePlayerToFaceMarker();

        PlayGnarlySoundFromMonsterDirection();
        StartCoroutine(DuckAudioSource(outdoorAmbient, 0.15f, 1.5f));

        yield return new WaitForSeconds(gnarlySoundHoldDuration);

        // === ACT 3: HEARTBEAT + DREAD BUILD ===
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


        if (useFogDuringChase) ApplyHorrorFog();

        monster.transform.position = doorMarker.position;
        FacePlayerHorizontal();
        monster.SetActive(true);
        monsterAnimator.applyRootMotion = false;
        monsterAnimator.Play(idleStateName, 0, 0f);

        if (shadowBacklight != null) shadowBacklight.enabled = true;
        if (shadowReveal != null) shadowReveal.Play();

        if (heartbeatAudio != null) StartCoroutine(RampPitch(heartbeatAudio, 1.3f, blackHoldDuration));

        yield return new WaitForSeconds(blackHoldDuration);

        // === ACT 6: FADE IN - shadow monster visible ===
        Debug.LogError("[Act 6] Fade in - shadow reveal");
        yield return StartCoroutine(FadeColor(new Color(0, 0, 0, 1), new Color(0, 0, 0, 0), fadeInDuration));

        StartCoroutine(PulseRedVignette());
        yield return new WaitForSeconds(shadowRevealDuration);

        // === ACT 7: CHARGE BEGINS ===
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

            timer += Time.deltaTime;
            yield return null;
        }

        // === ACT 8: POSITION FOR ATTACK ===
        Debug.LogError("[Act 8] Position attack");
        PositionMonsterForAttack();

        if (chaseAudio != null && chaseAudio.isPlaying) chaseAudio.Stop();
        if (heartbeatAudio != null && heartbeatAudio.isPlaying) heartbeatAudio.Stop();
        if (lowRumble != null && lowRumble.isPlaying) lowRumble.Stop();

        // === ACT 9: SILENT BEAT ===
        yield return new WaitForSeconds(silenceBeforeAttack);

        // === ACT 10: ATTACK - all audio hits at once ===
        Debug.LogError("[Act 10] ATTACK");
        monsterAnimator.Play(attackStateName, 0, 0f);
        if (attackBoom != null) attackBoom.Play();
        if (attackScreech != null) attackScreech.Play();

        StartCoroutine(CameraShakeBurst(cameraShakeAttackIntensity, redFlashDuration));
        yield return StartCoroutine(RedFlashAndFillVision());

        // === ACT 11: HOLD ON BLACK ===
        isShakingCamera = false;
        yield return new WaitForSeconds(holdOnBlackDuration);

        OnFinaleComplete();
    }

    // ============================================================
    // CALIBRATION TEST
    // ============================================================
    private void TestAttackPosition()
    {
        FindRuntimeReferences();

        if (monster == null || playerCamera == null)
        {
            Debug.LogError("[CALIBRATE] missing refs");
            return;
        }

        monster.SetActive(true);
        monsterAnimator.applyRootMotion = false;
        monsterAnimator.Play(idleStateName, 0, 0f);

        PositionMonsterForAttack();

        Debug.LogError("[CALIBRATE] ===== RESULTS =====");
        Debug.LogError("[CALIBRATE] Cam Y: " + playerCamera.position.y +
                       " | Monster Y: " + monster.transform.position.y);
    }

    // ============================================================
    // GNARLY SOUND POSITIONING (behind player)
    // ============================================================
    private void PlayGnarlySoundFromMonsterDirection()
    {
        if (gnarlyBehindSound == null) return;

        // Sound comes from the DoorMarker position (where monster will spawn)
        // After rotation, this is to the player's RIGHT
        gnarlyBehindSound.transform.position = doorMarker.position;
        gnarlyBehindSound.spatialBlend = 1f;
        gnarlyBehindSound.Play();

        Debug.LogError("[WheelchairFinale] Gnarly sound playing from monster direction");
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

        // Manual override wins if ticked
        if (useManualHeight)
        {
            Vector3 adjusted = monster.transform.position;
            adjusted.y = playerCamera.position.y + manualHeightOverride;
            monster.transform.position = adjusted;
            return;
        }

        // Head bone alignment if available
        if (monsterHeadBone != null)
        {
            monsterAnimator.Update(0f);
            float headYDelta = monsterHeadBone.position.y - monster.transform.position.y;
            float correctY = playerCamera.position.y - headYDelta + finalHeightOffset;

            Vector3 adjusted = monster.transform.position;
            adjusted.y = correctY;
            monster.transform.position = adjusted;
        }
        // Otherwise monster stays at its current Y (ground level from chase)
    }

    // ============================================================
    // LIGHTING TRANSFORMATION
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
                    if (l == shadowBacklight) continue;
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
        yield return new WaitForSeconds(shadowRevealDuration + fadeInDuration);

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
        Debug.LogError("[FOG] density=" + chaseFogDensity + " color=" + chaseFogColor);
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
    // CAMERA SHAKE (VR-safe, via rig offset)
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

    private void RotatePlayerToFaceMarker()
    {
        // Direction from player to the marker
        Vector3 dir = doorMarker.position - playerRig.position;
        dir.y = 0;
        dir.Normalize();

        if (dir.sqrMagnitude > 0.001f)
        {
            // Get the rotation that would face the marker
            Quaternion faceMarker = Quaternion.LookRotation(dir);

            // Rotate 90 degrees LEFT so the marker is to the player's RIGHT
            // (player has to turn right to see the monster)
            Quaternion offset = Quaternion.Euler(0, -90f, 0);
            playerRig.rotation = faceMarker * offset;

            Debug.LogError("[WheelchairFinale] Player rotated so monster is 90° to the right");
        }
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

        // Fallback: create a runtime canvas if no fade image was found
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