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
    [Tooltip("Name of the GameObject in your scene where the monster spawns from.")]
    [SerializeField] private string spawnPointName = "DoorMarker";

    [Header("=== FLICKER LIGHTS ===")]
    [SerializeField] private Light[] flickerLights;
    [SerializeField] private float flickerDuration = 0.5f;
    [Tooltip("Color to flicker the lights to before they cut out.")]
    [SerializeField] private Color flickerRedColor = new Color(1f, 0.1f, 0.1f);

    [Header("=== AUDIO ===")]
    [SerializeField] private AudioSource crashSound;
    [SerializeField] private AudioSource heartbeatAudio;
    [SerializeField] private AudioSource subBassRumble;
    [SerializeField] private AudioSource chaseAudio;
    [SerializeField] private AudioSource attackBoom;
    [SerializeField] private AudioSource attackScreech;

    [Header("=== TIMING ===")]
    [SerializeField] private float crashHoldDuration = 0.5f;
    [SerializeField] private float dreadBuildDuration = 2.5f;
    [SerializeField] private float fadeOutDuration = 0.3f;
    [SerializeField] private float blackHoldDuration = 0.3f;
    [SerializeField] private float fadeInDuration = 0.3f;
    [SerializeField] private float silenceBeforeAttack = 0.05f;

    [Header("=== CHASE ===")]
    [Tooltip("How close monster gets before stopping for attack.")]
    [SerializeField] private float attackTriggerDistance = 3.5f;
    [SerializeField] private float maxChaseTime = 5f;

    [Header("=== ATTACK POSITION (CRITICAL FOR SEATED VR) ===")]
    [Tooltip("How far in front of player camera the monster ends up.")]
    [SerializeField] private float attackDistanceFromPlayer = 1.8f;
    [Tooltip("Vertical offset from camera Y. Negative = lower. Try -0.3 to -0.7.")]
    [SerializeField] private float attackHeightOffset = -0.4f;

    [Header("=== RED LIGHTING ===")]
    [SerializeField] private Color redAmbientColor = new Color(0.5f, 0.05f, 0.05f);
    [SerializeField] private float redAmbientIntensity = 0.6f;
    [SerializeField] private string[] scenesToDarken = { "final_jumpscare", "Second Area" };

    [Header("=== IMPACT ===")]
    [SerializeField] private float redFlashDuration = 0.3f;
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
        else Debug.LogError("[WheelchairFinale] Could not find spawn point: " + spawnPointName);
    }

    // ============================================================
    // MAIN SEQUENCE
    // ============================================================
    private IEnumerator PlayScare()
    {
        FindRuntimeReferences();
        EnsureFadeImageExists();

        if (redVignetteImage != null) redVignetteImage.color = new Color(0.9f, 0, 0, 0);

        if (monster == null || monsterAnimator == null || playerCamera == null ||
            playerRig == null || agent == null || spawnPoint == null)
        {
            Debug.LogError("[WheelchairFinale] Missing critical refs - aborting");
            yield break;
        }

        // === STEP 1: TRAP ===
        Debug.Log("[Step 1] Crash + lock + flicker + spawn monster");
        DisableControllers();

        if (crashSound != null) crashSound.Play();
        StartCoroutine(FlickerLightsRedThenOff(flickerDuration));

        SpawnMonsterAtChosenPoint();
        monster.SetActive(true);
        monsterAnimator.applyRootMotion = false;
        monsterAnimator.Play(idleStateName, 0, 0f);

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
        {
            subBassRumble.volume = 0.2f;
            subBassRumble.Play();
            StartCoroutine(RampVolume(subBassRumble, 0.9f, dreadBuildDuration));
        }

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

        // Fade in DURING the chase
        StartCoroutine(FadeColor(new Color(0, 0, 0, 1), new Color(0, 0, 0, 0), fadeInDuration));

        // // === STEP 5: NAVMESH CHASE - target the EXACT attack position ===
        float timer = 0f;

        // Calculate the perfect attack position ONCE (in front of player camera)
        Vector3 perfectAttackPos = CalculatePerfectAttackPosition();
        Debug.Log("[CHASE] Targeting perfect attack position: " + perfectAttackPos);

        while (timer < maxChaseTime)
        {
            // Make monster path to the perfect attack spot, not just "near the player"
            agent.SetDestination(perfectAttackPos);

            // Check distance from monster to the TARGET position (not player)
            float distanceToTarget = Vector3.Distance(monster.transform.position, perfectAttackPos);
            Debug.Log("[CHASE] Distance to attack spot: " + distanceToTarget);

            // The moment monster reaches the attack spot, stop
            if (distanceToTarget <= 0.5f)
            {
                Debug.Log("[CHASE] Reached attack spot - attacking now");
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

        // === IMMEDIATE ATTACK - monster is already in the perfect position ===
        Debug.Log("[ATTACK] Seamless attack from perfect position");

        // Stop chase audio
        if (chaseAudio != null && chaseAudio.isPlaying) chaseAudio.Stop();
        if (heartbeatAudio != null && heartbeatAudio.isPlaying) heartbeatAudio.Stop();
        if (subBassRumble != null && subBassRumble.isPlaying) subBassRumble.Stop();

        // Tiny height correction for seated VR (no horizontal teleport - monster's already in the right spot)
        Vector3 finalPos = monster.transform.position;
        finalPos.y = playerCamera.position.y + attackHeightOffset;
        monster.transform.position = finalPos;

        // Make sure facing player
        FaceMonsterAtPlayer();

        // INSTANT attack
        monsterAnimator.CrossFadeInFixedTime(attackStateName, 0f, 0, 0f);
        if (attackBoom != null) attackBoom.Play();
        if (attackScreech != null) attackScreech.Play();
        StartCoroutine(ImpactShake(impactShakeIntensity, 0.2f));

        // === BULLETPROOF AGENT STOP ===
        agent.isStopped = true;
        agent.speed = 0;
        agent.velocity = Vector3.zero;
        agent.ResetPath();
        agent.enabled = false;

        // === STEP 6: ATTACK + INSTANT BLACK CUT (hides the snap) ===
        Debug.Log("[Step 6] Attack + cut to black");

        // Stop all chase audio first
        if (chaseAudio != null && chaseAudio.isPlaying) chaseAudio.Stop();
        if (heartbeatAudio != null && heartbeatAudio.isPlaying) heartbeatAudio.Stop();
        if (subBassRumble != null && subBassRumble.isPlaying) subBassRumble.Stop();

        // Snap monster + trigger attack + impact sounds + screen flash all in same frame
        SnapMonsterToAttackPositionInFrontOfPlayer();
        monsterAnimator.Play(attackStateName, 0, 0f);
        if (attackBoom != null) attackBoom.Play();
        if (attackScreech != null) attackScreech.Play();
        StartCoroutine(ImpactShake(impactShakeIntensity, 0.2f));

        // Very fast fade to black - hides the snap teleport
        yield return StartCoroutine(FadeColor(new Color(0, 0, 0, 0), new Color(0, 0, 0, 1), 0.15f));

        // Quick impact shake
        StartCoroutine(ImpactShake(impactShakeIntensity, 0.2f));

        // Fast fade to black - happens during the impact
        yield return StartCoroutine(FadeColor(new Color(0, 0, 0, 0), new Color(0, 0, 0, 1), 0.2f));

        // Stop ALL audio for dramatic silence
        if (attackBoom != null) attackBoom.Stop();
        if (attackScreech != null) attackScreech.Stop();

        // === STEP 9: HOLD ON BLACK FOREVER ===
        isShakingCamera = false;

        // Make sure the screen is fully black and stays that way
        if (fadeImage != null) fadeImage.color = new Color(0, 0, 0, 1);

        // Hold black indefinitely - game effectively ends here
        yield return new WaitForSeconds(holdOnBlackDuration);

        OnFinaleComplete();
    }

    private Vector3 CalculatePerfectAttackPosition()
    {
        if (playerCamera == null) return Vector3.zero;

        // Place the perfect attack position in front of the player camera
        Vector3 playerForward = playerCamera.forward;
        playerForward.y = 0;
        playerForward.Normalize();

        Vector3 attackPos = playerCamera.position + playerForward * attackDistanceFromPlayer;

        // Snap to NavMesh so the agent can actually reach it
        NavMeshHit hit;
        if (NavMesh.SamplePosition(attackPos, out hit, 3f, NavMesh.AllAreas))
        {
            attackPos = hit.position;
        }

        return attackPos;
    }
    // ============================================================
    // SPAWN AT CHOSEN POINT
    // ============================================================
    private void SpawnMonsterAtChosenPoint()
    {
        if (spawnPoint == null) return;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(spawnPoint.position, out hit, 5f, NavMesh.AllAreas))
        {
            monster.transform.position = hit.position;
        }
        else
        {
            monster.transform.position = spawnPoint.position;
            Debug.LogWarning("[SPAWN] Spawn point not on NavMesh, using raw position");
        }

        FaceMonsterAtPlayer();
        Debug.Log("[SPAWN] Monster placed at " + monster.transform.position);
    }

    // ============================================================
    // ROTATE PLAYER (rotation only, no teleport)
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

        Debug.Log("[ROTATE] Player rig rotated by " + angleDifference + "°");
    }

    // ============================================================
    // SNAP TO ATTACK POSITION
    // ============================================================
    private void SnapMonsterToAttackPositionInFrontOfPlayer()
    {
        if (monster == null || playerCamera == null) return;

        Vector3 playerForward = playerCamera.forward;
        playerForward.y = 0;
        playerForward.Normalize();

        Vector3 targetPos = playerCamera.position + playerForward * attackDistanceFromPlayer;

        // Ground via NavMesh
        NavMeshHit hit;
        if (NavMesh.SamplePosition(targetPos, out hit, 3f, NavMesh.AllAreas))
        {
            targetPos.y = hit.position.y;
        }
        else
        {
            targetPos.y = monster.transform.position.y;
        }

        // Apply seated VR height offset
        targetPos.y += attackHeightOffset;

        monster.transform.position = targetPos;
        FaceMonsterAtPlayer();

        Debug.Log("[ATTACK POS] Monster snapped to: " + targetPos);
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
    // CALIBRATION TEST (press C)
    // ============================================================
    private void TestAttackPosition()
    {
        FindRuntimeReferences();
        if (monster == null || playerCamera == null) return;

        monster.SetActive(true);
        monsterAnimator.applyRootMotion = false;
        monsterAnimator.Play(attackStateName, 0, 0f);

        SnapMonsterToAttackPositionInFrontOfPlayer();

        Debug.Log("[CALIBRATE] Adjust attackHeightOffset and press C again.");
    }

    // ============================================================
    // RED LIGHTING (ambient)
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
    // FLICKER LIGHTS RED THEN OFF
    // ============================================================
    private IEnumerator FlickerLightsRedThenOff(float duration)
    {
        if (flickerLights == null || flickerLights.Length == 0) yield break;

        Color[] originalColors = new Color[flickerLights.Length];
        bool[] originalStates = new bool[flickerLights.Length];

        for (int i = 0; i < flickerLights.Length; i++)
        {
            if (flickerLights[i] != null)
            {
                originalColors[i] = flickerLights[i].color;
                originalStates[i] = flickerLights[i].enabled;
            }
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            for (int i = 0; i < flickerLights.Length; i++)
            {
                Light l = flickerLights[i];
                if (l == null) continue;

                float roll = Random.value;
                if (roll < 0.5f)
                {
                    l.enabled = true;
                    l.color = originalColors[i];
                }
                else if (roll < 0.8f)
                {
                    l.enabled = true;
                    l.color = flickerRedColor;
                }
                else
                {
                    l.enabled = false;
                }
            }

            float wait = Random.Range(0.04f, 0.12f);
            elapsed += wait;
            yield return new WaitForSeconds(wait);
        }

        for (int i = 0; i < flickerLights.Length; i++)
        {
            if (flickerLights[i] != null)
            {
                flickerLights[i].color = originalColors[i];
                flickerLights[i].enabled = false;
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
    // FADE / IMPACT
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

    private IEnumerator RedFlashAndFillVision()
    {
        if (fadeImage == null) yield break;

        Color red = new Color(0.9f, 0f, 0f, 0.8f);
        Color black = new Color(0, 0, 0, 1);
        fadeImage.color = new Color(0, 0, 0, 0);

        Vector3 monsterStart = monster.transform.position;
        Vector3 cameraPos = playerCamera.position;
        cameraPos.y = monsterStart.y;

        float elapsed = 0f;
        while (elapsed < redFlashDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / redFlashDuration;

            monster.transform.position = Vector3.Lerp(monsterStart, cameraPos, t);
            FaceMonsterAtPlayer();

            if (t < 0.5f) fadeImage.color = Color.Lerp(new Color(0, 0, 0, 0), red, t * 2f);
            else fadeImage.color = Color.Lerp(red, black, (t - 0.5f) * 2f);

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
        Debug.Log("[WheelchairFinale] complete");
    }
}