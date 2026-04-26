using Bhaptics.SDK2;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
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
    [SerializeField] private NavMeshAgent agent;

    [Header("=== ANIMATOR STATES ===")]
    [SerializeField] private string chaseStateName = "Chase";
    [SerializeField] private string attackStateName = "Attack";
    [SerializeField] private string idleStateName = "Idle";

    [Header("=== CROSS-SCENE REFERENCES ===")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private string redVignetteName = "RedVignette";
    [SerializeField] private string fadeImageName = "FadeImage";
    [SerializeField] private string spawnPointName = "DoorMarker";
    [SerializeField] private string horrorPostFXName = "HorrorPostFX";

    [Header("=== FLICKER LIGHTS ===")]
    [SerializeField] private Light[] flickerLights;
    [SerializeField] private float flickerDuration = 0.6f;
    [SerializeField] private Color flickerRedColor = new Color(1f, 0.1f, 0.1f);

    [Header("=== AUDIO ===")]
    [SerializeField] private AudioSource crashSound;
    [SerializeField] private AudioSource heartbeatAudio;
    [SerializeField] private AudioSource subBassRumble;
    [SerializeField] private AudioSource chaseAudio;
    [SerializeField] private AudioSource attackBoom;
    [SerializeField] private AudioSource attackScreech;

    [Header("=== TIMING ===")]
    [SerializeField] private float falseSafetyDuration = 3f;
    [SerializeField] private float crashHoldDuration = 0.5f;
    [SerializeField] private float dreadBuildDuration = 2.5f;
    [SerializeField] private float fadeOutDuration = 0.3f;
    [SerializeField] private float blackHoldDuration = 0.3f;
    [SerializeField] private float fadeInDuration = 0.3f;
    [SerializeField] private float silenceBeforeAttack = 0.2f;

    [Header("=== CHASE ===")]
    [SerializeField] private float maxChaseTime = 5f;
    [SerializeField] private float chaseSpeed = 12f;
    [SerializeField] private float chaseAcceleration = 20f;

    [Header("=== ATTACK POSITION ===")]
    [Tooltip("How far in front of player camera the monster stops.")]
    [SerializeField] private float attackDistanceFromPlayer = 1.8f;
    [Tooltip("HOW FAR BELOW THE CAMERA the monster ROOT should be. " +
             "Press C to test. Start at -1.0 and adjust. " +
             "More negative = monster drops lower. Less negative = rises higher.")]
    [SerializeField] private float attackHeightOffset = -1.0f;

    [Header("=== POST-PROCESSING ===")]
    [SerializeField] private float postFXRampDuration = 1f;

    [Header("=== RED LIGHTING ===")]
    [SerializeField] private Color redAmbientColor = new Color(0.5f, 0.05f, 0.05f);
    [SerializeField] private float redAmbientIntensity = 0.6f;
    [SerializeField] private string[] scenesToDarken = { "final_jumpscare", "Second Area" };

    [Header("=== IMPACT ===")]
    [SerializeField] private float holdOnBlackDuration = 4f;

    [Header("=== CAMERA SHAKE ===")]
    [SerializeField] private float chaseShakeIntensity = 0.015f;
    [SerializeField] private float impactShakeIntensity = 0.08f;

    [Header("=== CALIBRATION ===")]
    [Tooltip("Press C in play mode to test attack position instantly.")]
    [SerializeField] private bool enableCalibrationMode = true;

    // Runtime
    private Transform playerCamera, playerRig;
    private Transform spawnPoint;
    private ContinuousMoveProvider moveProvider;
    private ContinuousTurnProvider turnProvider;
    private SnapTurnProvider snapTurnProvider;
    private Image redVignetteImage;
    private Image fadeImage;
    private Volume horrorPostFX;

    // State
    private bool hasTriggered = false;
    private bool isShakingCamera = false;
    private Vector3 rigShakeBasePos;
    private List<Light> disabledLights = new List<Light>();

    private void Awake()
    {
        if (monster != null) monster.SetActive(false);
        if (agent != null) agent.enabled = false;
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

        GameObject vignette = GameObject.Find(redVignetteName);
        if (vignette != null) redVignetteImage = vignette.GetComponent<Image>();

        GameObject fade = GameObject.Find(fadeImageName);
        if (fade != null) fadeImage = fade.GetComponent<Image>();

        GameObject sp = GameObject.Find(spawnPointName);
        if (sp != null) spawnPoint = sp.transform;
        else Debug.LogError("[WheelchairFinale] Could not find: " + spawnPointName);

        GameObject postFX = GameObject.Find(horrorPostFXName);
        if (postFX != null) horrorPostFX = postFX.GetComponent<Volume>();
    }

    // ============================================================
    // THE SINGLE SOURCE OF TRUTH FOR ATTACK POSITION
    // Both calibration AND the real scare call this same method
    // ============================================================
    private void PlaceMonsterAtAttackPosition()
    {
        if (monster == null || playerCamera == null) return;

        // Get horizontal direction player is looking
        Vector3 playerForward = playerCamera.forward;
        playerForward.y = 0;
        playerForward.Normalize();

        // XZ position: directly in front of player
        Vector3 targetPos = playerCamera.position + playerForward * attackDistanceFromPlayer;

        // Y position: camera height + offset
        // attackHeightOffset controls how far BELOW the camera the monster ROOT sits
        // The monster's head is ABOVE the root, so this offset needs to be negative enough
        // to account for the monster's height
        targetPos.y = playerCamera.position.y + attackHeightOffset;

        monster.transform.position = targetPos;
        FaceMonsterAtPlayer();

        Debug.Log("[ATTACK POS] Camera Y: " + playerCamera.position.y +
                  " | Monster root Y: " + targetPos.y +
                  " | Offset: " + attackHeightOffset);
    }

    // ============================================================
    // CHASE TARGET (XZ only - we handle Y separately at attack time)
    // ============================================================
    private Vector3 CalculateChaseTarget()
    {
        if (playerCamera == null) return Vector3.zero;

        Vector3 playerForward = playerCamera.forward;
        playerForward.y = 0;
        playerForward.Normalize();

        // Target is in front of player on the ground plane
        Vector3 chaseTarget = playerCamera.position + playerForward * attackDistanceFromPlayer;

        // Snap XZ to NavMesh so agent can reach it
        NavMeshHit hit;
        if (NavMesh.SamplePosition(chaseTarget, out hit, 3f, NavMesh.AllAreas))
        {
            chaseTarget = hit.position; // NavMesh Y (floor level)
        }

        return chaseTarget;
    }

    // ============================================================
    // MAIN SEQUENCE
    // ============================================================
    private IEnumerator PlayScare()
    {
        FindRuntimeReferences();
        EnsureFadeImageExists();

        if (redVignetteImage != null) redVignetteImage.color = new Color(0.9f, 0, 0, 0);
        if (horrorPostFX != null) horrorPostFX.weight = 0f;

        if (monster == null || monsterAnimator == null || playerCamera == null ||
            playerRig == null || agent == null || spawnPoint == null)
        {
            Debug.LogError("[WheelchairFinale] Missing critical refs - aborting");
            yield break;
        }

        // === FALSE SAFETY - player thinks they escaped ===
        Debug.Log("[False Safety] Player thinks they're free");
        yield return new WaitForSeconds(falseSafetyDuration);

        // === STEP 1: TRAP ===
        Debug.Log("[Step 1] Crash + lock + flicker + spawn monster");
        DisableControllers();

        if (crashSound != null) crashSound.Play();
        StartCoroutine(FlickerLightsRedThenOff(flickerDuration));

        // Sub bass starts immediately with the crash
        if (subBassRumble != null)
        {
            subBassRumble.volume = 0.15f;
            subBassRumble.Play();
        }

        SpawnMonsterAtChosenPoint();
        monster.SetActive(true);
        monsterAnimator.applyRootMotion = false;
        monsterAnimator.Play(idleStateName, 0, 0f);

        StartCoroutine(RampPostFX(1f, postFXRampDuration));

        yield return new WaitForSeconds(crashHoldDuration);

        // === STEP 2: HEARTBEAT + DREAD ===
        Debug.Log("[Step 2] Heartbeat + dread builds");

        if (heartbeatAudio != null)
        {
            heartbeatAudio.pitch = 1f;
            heartbeatAudio.Play();
            StartCoroutine(RampPitch(heartbeatAudio, 1.6f, dreadBuildDuration));
        }

        if (subBassRumble != null)
            StartCoroutine(RampVolume(subBassRumble, 0.9f, dreadBuildDuration));

        ApplyRedHorrorLighting();
        StartCoroutine(PulseRedVignette());

        yield return new WaitForSeconds(dreadBuildDuration);

        // === STEP 3: FADE TO BLACK + ROTATE PLAYER ===
        Debug.Log("[Step 3] Fade + rotate to face monster");
        yield return StartCoroutine(FadeColor(new Color(0, 0, 0, 0), new Color(0, 0, 0, 1), fadeOutDuration));
        RotatePlayerToFaceSpawnPoint();
        yield return new WaitForSeconds(blackHoldDuration);

        // === STEP 4: CHASE BEGINS WHILE STILL BLACK ===
        Debug.Log("[Step 4] Chase starts while still in black");
        monsterAnimator.Play(chaseStateName, 0, 0f);
        if (chaseAudio != null) chaseAudio.Play();

        rigShakeBasePos = playerRig.localPosition;
        isShakingCamera = true;
        StartCoroutine(CameraShakeLoop(chaseShakeIntensity));

        agent.enabled = true;
        agent.isStopped = false;
        agent.speed = chaseSpeed;
        agent.acceleration = chaseAcceleration;
        agent.angularSpeed = 360f;

        // Fade in during chase - monster already moving when revealed
        StartCoroutine(FadeColor(new Color(0, 0, 0, 1), new Color(0, 0, 0, 0), fadeInDuration));

        // === STEP 5: NAVMESH CHASE ===
        // Monster chases to a NavMesh-valid point in front of player
        // Y will be floor level from NavMesh - that's fine, we fix it at attack time
        Vector3 chaseTarget = CalculateChaseTarget();
        Debug.Log("[CHASE] Target: " + chaseTarget);

        float timer = 0f;
        while (timer < maxChaseTime)
        {
            agent.SetDestination(chaseTarget);

            float distanceToTarget = Vector3.Distance(
                new Vector3(monster.transform.position.x, 0, monster.transform.position.z),
                new Vector3(chaseTarget.x, 0, chaseTarget.z));

            if (distanceToTarget <= 0.5f)
            {
                Debug.Log("[CHASE] Reached attack spot");
                break;
            }

            timer += Time.deltaTime;
            yield return null;
        }

        // === BULLETPROOF AGENT STOP ===
        agent.isStopped = true;
        agent.speed = 0;
        agent.velocity = Vector3.zero;
        agent.ResetPath();
        agent.enabled = false;

        // === STEP 6: ATTACK ===
        Debug.Log("[Step 6] Attack");

        if (chaseAudio != null && chaseAudio.isPlaying) chaseAudio.Stop();
        if (heartbeatAudio != null && heartbeatAudio.isPlaying) heartbeatAudio.Stop();
        if (subBassRumble != null && subBassRumble.isPlaying) subBassRumble.Stop();

        // FORCE THE MONSTER TO ATTACK POSITION - this is the version that worked!
        // It snaps to be in front of the player at correct seated height
        SnapMonsterToAttackPositionInFrontOfPlayer();

        // INSTANT attack - all same frame as the snap (player won't see the snap because everything happens together)
        monsterAnimator.CrossFadeInFixedTime(attackStateName, 0f, 0, 0f);
        if (attackBoom != null) attackBoom.Play();
        if (attackScreech != null) attackScreech.Play();
        StartCoroutine(ImpactShake(impactShakeIntensity, 0.2f));
        BhapticsLibrary.Play("hunt_vibration");

        // Cut to black IMMEDIATELY after the snap+attack - hides any visible repositioning
        yield return StartCoroutine(FadeColor(new Color(0, 0, 0, 0), new Color(0, 0, 0, 1), 0.1f));

        // === STEP 7: HOLD ON BLACK ===
        isShakingCamera = false;
        if (attackBoom != null) attackBoom.Stop();
        if (attackScreech != null) attackScreech.Stop();
        if (fadeImage != null) fadeImage.color = new Color(0, 0, 0, 1);

        yield return new WaitForSeconds(holdOnBlackDuration);

        OnFinaleComplete();
    }

    // ============================================================
    // SNAP MONSTER TO ATTACK POSITION (the version that worked perfectly)
    // ============================================================
    private void SnapMonsterToAttackPositionInFrontOfPlayer()
    {
        if (monster == null || playerCamera == null) return;

        // Direction the player is currently looking (horizontal only)
        Vector3 playerForward = playerCamera.forward;
        playerForward.y = 0;
        playerForward.Normalize();

        // Position: directly in front of player at attack distance
        Vector3 targetPos = playerCamera.position + playerForward * attackDistanceFromPlayer;

        // Height: camera Y + offset (this is what fixes "attack above head")
        targetPos.y = playerCamera.position.y + attackHeightOffset;

        monster.transform.position = targetPos;
        FaceMonsterAtPlayer();

        Debug.Log("[ATTACK SNAP] Camera Y: " + playerCamera.position.y +
                  " | Monster Y: " + targetPos.y +
                  " | Offset: " + attackHeightOffset);
    }

    // ============================================================
    // SPAWN AT CHOSEN POINT
    // ============================================================
    private void SpawnMonsterAtChosenPoint()
    {
        if (spawnPoint == null) return;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(spawnPoint.position, out hit, 5f, NavMesh.AllAreas))
            monster.transform.position = hit.position;
        else
            monster.transform.position = spawnPoint.position;

        FaceMonsterAtPlayer();
        Debug.Log("[SPAWN] Monster placed at " + monster.transform.position);
    }
    // ============================================================
    // SNAP MONSTER TO ATTACK POSITION (the version that worked perfectly)
    // ============================================================
    
    // ============================================================
    // ROTATE PLAYER
    // ============================================================
    private void RotatePlayerToFaceSpawnPoint()
    {
        if (playerRig == null || spawnPoint == null || playerCamera == null) return;

        Vector3 targetDir = spawnPoint.position - playerCamera.position;
        targetDir.y = 0;
        if (targetDir.sqrMagnitude < 0.001f) return;
        targetDir.Normalize();

        Vector3 currentCamDir = playerCamera.forward;
        currentCamDir.y = 0;
        currentCamDir.Normalize();

        float angleDifference = Vector3.SignedAngle(currentCamDir, targetDir, Vector3.up);
        playerRig.Rotate(Vector3.up, angleDifference, Space.World);
    }

    // ============================================================
    // FACE MONSTER AT PLAYER
    // ============================================================
    private void FaceMonsterAtPlayer()
    {
        if (monster == null || playerCamera == null) return;
        Vector3 lookPos = playerCamera.position;
        lookPos.y = monster.transform.position.y;
        monster.transform.LookAt(lookPos);
    }

    // ============================================================
    // CALIBRATION - uses EXACT same method as the real attack
    // ============================================================
    private void TestAttackPosition()
    {
        FindRuntimeReferences();
        if (monster == null || playerCamera == null) return;

        monster.SetActive(true);
        monsterAnimator.applyRootMotion = false;
        monsterAnimator.Play(attackStateName, 0, 0f);

        SnapMonsterToAttackPositionInFrontOfPlayer();

        Debug.Log("[CALIBRATE] Camera Y: " + playerCamera.position.y +
                  " | Monster Y: " + monster.transform.position.y +
                  " | Adjust attackHeightOffset and press C again");
    }

    // ============================================================
    // POST-PROCESSING
    // ============================================================
    private IEnumerator RampPostFX(float targetWeight, float duration)
    {
        if (horrorPostFX == null) yield break;
        float startWeight = horrorPostFX.weight;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            horrorPostFX.weight = Mathf.Lerp(startWeight, targetWeight, elapsed / duration);
            yield return null;
        }
        horrorPostFX.weight = targetWeight;
    }

    // ============================================================
    // RED LIGHTING
    // ============================================================
    private void ApplyRedHorrorLighting()
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
                    if (!l.enabled) continue;
                    l.enabled = false;
                    disabledLights.Add(l);
                }
            }
        }

        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = redAmbientColor;
        RenderSettings.ambientIntensity = redAmbientIntensity;
    }

    // ============================================================
    // CINEMATIC FLICKER
    // ============================================================
    private IEnumerator FlickerLightsRedThenOff(float duration)
    {
        if (flickerLights == null || flickerLights.Length == 0) yield break;

        Color[] originalColors = new Color[flickerLights.Length];
        float[] originalIntensities = new float[flickerLights.Length];

        for (int i = 0; i < flickerLights.Length; i++)
        {
            if (flickerLights[i] != null)
            {
                originalColors[i] = flickerLights[i].color;
                originalIntensities[i] = flickerLights[i].intensity;
            }
        }

        SetAllLights(false, originalColors, originalIntensities);
        yield return new WaitForSeconds(0.1f);

        SetAllLights(true, originalColors, originalIntensities);
        yield return new WaitForSeconds(0.2f);

        for (int i = 0; i < flickerLights.Length; i++)
        {
            if (flickerLights[i] == null) continue;
            flickerLights[i].color = flickerRedColor;
            flickerLights[i].intensity = originalIntensities[i] * 0.5f;
        }
        yield return new WaitForSeconds(0.08f);

        SetAllLights(false, originalColors, originalIntensities);
        yield return new WaitForSeconds(0.05f);

        for (int i = 0; i < flickerLights.Length; i++)
        {
            if (flickerLights[i] == null) continue;
            flickerLights[i].enabled = true;
            flickerLights[i].color = originalColors[i];
            flickerLights[i].intensity = originalIntensities[i] * 0.7f;
        }
        yield return new WaitForSeconds(0.15f);

        float remaining = duration - 0.58f;
        if (remaining > 0)
        {
            float elapsed = 0f;
            while (elapsed < remaining)
            {
                for (int i = 0; i < flickerLights.Length; i++)
                {
                    if (flickerLights[i] == null) continue;
                    bool on = Random.value > 0.4f;
                    flickerLights[i].enabled = on;
                    if (on)
                    {
                        flickerLights[i].color = Random.value > 0.5f ? flickerRedColor : originalColors[i];
                        flickerLights[i].intensity = originalIntensities[i] * Random.Range(0.3f, 0.8f);
                    }
                }
                float wait = Random.Range(0.04f, 0.08f);
                elapsed += wait;
                yield return new WaitForSeconds(wait);
            }
        }

        for (int i = 0; i < flickerLights.Length; i++)
            if (flickerLights[i] != null) flickerLights[i].enabled = false;
    }

    private void SetAllLights(bool on, Color[] originalColors, float[] originalIntensities)
    {
        for (int i = 0; i < flickerLights.Length; i++)
        {
            if (flickerLights[i] == null) continue;
            flickerLights[i].enabled = on;
            if (on)
            {
                flickerLights[i].color = originalColors[i];
                flickerLights[i].intensity = originalIntensities[i];
            }
        }
    }

    // ============================================================
    // VIGNETTE
    // ============================================================
    private IEnumerator PulseRedVignette()
    {
        if (redVignetteImage == null) yield break;

        Color off = new Color(0.9f, 0f, 0f, 0f);
        Color on = new Color(0.9f, 0f, 0f, 0.5f);

        float t = 0f;
        while (isShakingCamera || t < 5f)
        {
            t += Time.deltaTime;
            float pulse = (Mathf.Sin(t * 5f) + 1f) * 0.5f;
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

    private IEnumerator ImpactShake(float intensity, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float remaining = 1f - (elapsed / duration);
            Vector3 shake = Random.insideUnitSphere * intensity * remaining;
            shake.y *= 0.3f;
            playerRig.localPosition = rigShakeBasePos + shake;
            yield return null;
        }
        playerRig.localPosition = rigShakeBasePos;
    }

    // ============================================================
    // AUDIO HELPERS
    // ============================================================
    private IEnumerator RampVolume(AudioSource src, float targetVolume, float duration)
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
    // CONTROLS
    // ============================================================
    private void DisableControllers()
    {
        if (moveProvider != null) moveProvider.enabled = false;
        if (turnProvider != null) turnProvider.enabled = false;
        if (snapTurnProvider != null) snapTurnProvider.enabled = false;
    }

    // ============================================================
    // FADE
    // ============================================================
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
        Debug.Log("[WheelchairFinale] complete");
    }
}