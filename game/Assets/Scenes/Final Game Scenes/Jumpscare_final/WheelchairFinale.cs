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
    [Header("Monster")]
    [SerializeField] private GameObject monster;
    [SerializeField] private Animator monsterAnimator;

    [Header("Animator State Names")]
    [SerializeField] private string chaseStateName = "Chase";
    [SerializeField] private string attackStateName = "Attack";

    [Header("Cross-scene references - auto-found by name")]
    [SerializeField] private string doorMarkerName = "DoorMarker";
    [SerializeField] private string playerTag = "Player";

    [Header("Audio")]
    [SerializeField] private AudioSource crashOrCrySound;
    [SerializeField] private AudioSource revealStinger;
    [SerializeField] private AudioSource attackScreech;
    [Tooltip("Optional: low rumble/drone that plays during the chase.")]
    [SerializeField] private AudioSource chaseLowRumble;
    [Tooltip("Optional: heartbeat audio that plays during freeze phase.")]
    [SerializeField] private AudioSource heartbeatAudio;

    [Header("Sequence Timing")]
    [SerializeField] private float freezeDuration = 2f;
    [SerializeField] private float fadeOutDuration = 1.2f;
    [SerializeField] private float blackHoldDuration = 1.5f;
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private float startChaseSpeed = 5f;
    [SerializeField] private float maxChaseSpeed = 9f;
    [SerializeField] private float chaseAcceleration = 4f;
    [SerializeField] private float attackDistance = 1.8f;
    [SerializeField] private float maxChaseTime = 4f;

    [Header("Ground Snapping (chase only)")]
    [SerializeField] private LayerMask groundMask = ~0;
    [SerializeField] private float groundRayStartHeight = 2f;
    [SerializeField] private float groundRayDistance = 10f;

    [Header("SEATED PLAYER - Attack Position")]
    [SerializeField] private float attackStopDistance = 0.9f;
    [Tooltip("Tweak while testing: negative lowers the monster. For seated VR try -0.2 to -0.6.")]
    [SerializeField] private float seatedHeightOffset = -0.4f;
    [SerializeField] private float fallbackMonsterHeight = 2f;

    [Header("Horror Atmosphere")]
    [SerializeField] private string[] scenesToDarken = { "final_jumpscare", "Second Area" };
    [Tooltip("Optional spotlight on the monster (stays enabled during scare).")]
    [SerializeField] private Light monsterSpotlight;

    [Header("Red Horror Lighting")]
    [Tooltip("How red/dark the scene gets during the chase.")]
    [SerializeField] private Color chaseAmbientColor = new Color(0.15f, 0.02f, 0.02f);
    [SerializeField] private float chaseAmbientIntensity = 0.3f;
    [Tooltip("How fast the red lighting ramps up during chase.")]
    [SerializeField] private float redLightingRampDuration = 0.8f;

    [Header("Impact Effects")]
    [Tooltip("Red vignette that pulses during chase (separate from fade).")]
    [SerializeField] private Image redVignetteImage;
    [SerializeField] private float redFlashDuration = 0.25f;
    [SerializeField] private float holdOnBlackDuration = 3f;
    [SerializeField] private Image fadeImage;

    [Header("Camera Shake")]
    [SerializeField] private float cameraShakeIntensity = 0.05f;
    [SerializeField] private float cameraShakeChaseIntensity = 0.02f;

    // Runtime references
    private Transform playerCamera;
    private Transform playerRig;
    private Transform doorMarker;
    private ContinuousMoveProvider moveProvider;
    private ContinuousTurnProvider turnProvider;
    private SnapTurnProvider snapTurnProvider;

    // State
    private bool hasTriggered = false;
    private List<Light> disabledLights = new List<Light>();
    private Color cachedAmbientLight;
    private AmbientMode cachedAmbientMode;
    private float cachedAmbientIntensity;
    private float cachedReflectionIntensity;
    private float monsterHeight = 2f;
    private bool isShakingCamera = false;

    private void Awake()
    {
        Debug.LogError("[WheelchairFinale] Awake on " + gameObject.name);

        if (monster != null)
        {
            MeasureMonsterHeight();
            monster.SetActive(false);
        }

        if (monsterSpotlight != null) monsterSpotlight.enabled = false;

        // Ensure red vignette starts invisible
        if (redVignetteImage != null)
            redVignetteImage.color = new Color(0.8f, 0f, 0f, 0f);
    }

    private void MeasureMonsterHeight()
    {
        Renderer[] renderers = monster.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            Bounds combined = renderers[0].bounds;
            foreach (Renderer r in renderers) combined.Encapsulate(r.bounds);
            monsterHeight = combined.size.y;
            Debug.LogError("[WheelchairFinale] Measured monster height: " + monsterHeight);
        }
        else monsterHeight = fallbackMonsterHeight;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered) return;
        if (!other.CompareTag(playerTag)) return;

        Debug.LogError("[WheelchairFinale] trigger hit — starting scare");
        hasTriggered = true;
        StartCoroutine(PlayScare());
    }

    private void FindRuntimeReferences()
    {
        if (Camera.main != null) playerCamera = Camera.main.transform;

        GameObject playerObj = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObj != null)
        {
            playerRig = playerObj.transform;
            moveProvider = playerObj.GetComponentInChildren<ContinuousMoveProvider>();
            turnProvider = playerObj.GetComponentInChildren<ContinuousTurnProvider>();
            snapTurnProvider = playerObj.GetComponentInChildren<SnapTurnProvider>();
        }

        GameObject markerObj = GameObject.Find(doorMarkerName);
        if (markerObj != null) doorMarker = markerObj.transform;
    }

    private IEnumerator PlayScare()
    {
        FindRuntimeReferences();

        if (monster == null || monsterAnimator == null || playerCamera == null ||
            doorMarker == null || playerRig == null)
        {
            Debug.LogError("[WheelchairFinale] missing refs - aborting");
            yield break;
        }

        EnsureFadeImageExists();

        // === PHASE 1: FREEZE + HEARTBEAT ===
        Debug.LogError("[Phase 1] Freeze");
        DisableControllers();
        if (crashOrCrySound != null) crashOrCrySound.Play();
        if (heartbeatAudio != null) heartbeatAudio.Play();

        yield return new WaitForSeconds(freezeDuration);

        // === PHASE 2: FADE TO BLACK ===
        Debug.LogError("[Phase 2] Fade to black BEFORE rotating");
        yield return StartCoroutine(FadeColor(new Color(0, 0, 0, 0), new Color(0, 0, 0, 1), fadeOutDuration));

        // Stop heartbeat as we go into darkness
        if (heartbeatAudio != null && heartbeatAudio.isPlaying) heartbeatAudio.Stop();

        // === PHASE 3: WHILE BLACK - rotate, darken world, set up monster ===
        Debug.LogError("[Phase 3] In darkness: rotate + setup");
        RotatePlayerToFaceMarker();
        DarkenWorld();

        // Place monster EXACTLY at DoorMarker
        monster.transform.position = doorMarker.position;
        FacePlayerHorizontal();
        monster.SetActive(true);
        if (monsterSpotlight != null) monsterSpotlight.enabled = true;

        // Kill root motion so our script controls movement
        monsterAnimator.applyRootMotion = false;
        monsterAnimator.Play(chaseStateName, 0, 0f);

        if (revealStinger != null) revealStinger.Play();

        // Hold in darkness with the stinger
        yield return new WaitForSeconds(blackHoldDuration);

        // === PHASE 4: FADE IN - monster visible already charging ===
        Debug.LogError("[Phase 4] Fade back in - monster is there");
        yield return StartCoroutine(FadeColor(new Color(0, 0, 0, 1), new Color(0, 0, 0, 0), fadeInDuration));

        // === PHASE 5: RED HORROR LIGHTING RAMPS UP ===
        Debug.LogError("[Phase 5] Ramping red lighting");
        StartCoroutine(RampRedLighting());
        StartCoroutine(PulseRedVignette());

        // Start chase rumble
        if (chaseLowRumble != null) chaseLowRumble.Play();

        // Start subtle camera shake
        isShakingCamera = true;
        StartCoroutine(CameraShakeLoop(cameraShakeChaseIntensity));

        // === PHASE 6: THE CHASE ===
        Debug.LogError("[Phase 6] Chase");
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

        // === PHASE 7: POSITION FOR SEATED ATTACK (in your face!) ===
        Debug.LogError("[Phase 7] Positioning for seated attack");

        Vector3 dirToPlayer = (playerCamera.position - monster.transform.position);
        dirToPlayer.y = 0;
        dirToPlayer.Normalize();

        Vector3 attackPos = playerCamera.position - dirToPlayer * attackStopDistance;

        // SEATED VR FIX: head should be at camera level
        attackPos.y = playerCamera.position.y - (monsterHeight * 0.7f) + seatedHeightOffset;

        monster.transform.position = attackPos;
        FacePlayerHorizontal();

        Debug.LogError("[WheelchairFinale] Attack Pos: " + attackPos +
                       " | Cam: " + playerCamera.position + " | MHeight: " + monsterHeight);

        // Stop rumble, brief gasp of silence right before the scream
        if (chaseLowRumble != null && chaseLowRumble.isPlaying) chaseLowRumble.Stop();
        yield return new WaitForSeconds(0.08f);

        // === PHASE 8: ATTACK + BIG CAMERA SHAKE + SCREECH ===
        Debug.LogError("[Phase 8] ATTACK");
        monsterAnimator.Play(attackStateName, 0, 0f);
        if (attackScreech != null) attackScreech.Play();

        // Bigger shake during attack
        StartCoroutine(CameraShakeBurst(cameraShakeIntensity, redFlashDuration));

        yield return StartCoroutine(RedFlashAndFillVision());

        // === PHASE 9: HOLD ON BLACK ===
        isShakingCamera = false;
        yield return new WaitForSeconds(holdOnBlackDuration);

        OnFinaleComplete();
    }

    // ============================================================
    // HORROR LIGHTING
    // ============================================================

    private void DarkenWorld()
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
                    if (!l.enabled) continue;
                    l.enabled = false;
                    disabledLights.Add(l);
                }
            }
        }

        cachedAmbientMode = RenderSettings.ambientMode;
        cachedAmbientLight = RenderSettings.ambientLight;
        cachedAmbientIntensity = RenderSettings.ambientIntensity;
        cachedReflectionIntensity = RenderSettings.reflectionIntensity;

        // Start very dark - red lighting ramps in during chase
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.02f, 0.01f, 0.01f);
        RenderSettings.ambientIntensity = 0.05f;
        RenderSettings.reflectionIntensity = 0.1f;

        Debug.LogError("[WheelchairFinale] Darkened world - " + disabledLights.Count + " lights off");
    }

    private IEnumerator RampRedLighting()
    {
        Color startColor = RenderSettings.ambientLight;
        float startIntensity = RenderSettings.ambientIntensity;
        float elapsed = 0f;

        while (elapsed < redLightingRampDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / redLightingRampDuration;
            RenderSettings.ambientLight = Color.Lerp(startColor, chaseAmbientColor, t);
            RenderSettings.ambientIntensity = Mathf.Lerp(startIntensity, chaseAmbientIntensity, t);
            yield return null;
        }
    }

    private IEnumerator PulseRedVignette()
    {
        if (redVignetteImage == null) yield break;

        Color off = new Color(0.9f, 0f, 0f, 0f);
        Color on = new Color(0.9f, 0f, 0f, 0.45f);

        float t = 0f;
        while (isShakingCamera || t < 4f)
        {
            t += Time.deltaTime;
            // Pulse at ~2 Hz, slightly irregular feels more alive
            float pulse = (Mathf.Sin(t * 6f) + 1f) * 0.5f;
            redVignetteImage.color = Color.Lerp(off, on, pulse * 0.7f);
            yield return null;
        }

        redVignetteImage.color = off;
    }

    // ============================================================
    // CAMERA SHAKE (via rig offset - doesn't fight VR head tracking)
    // ============================================================

    private IEnumerator CameraShakeLoop(float intensity)
    {
        Vector3 basePos = playerRig.localPosition;
        while (isShakingCamera)
        {
            Vector3 shake = Random.insideUnitSphere * intensity;
            shake.y *= 0.3f; // less vertical shake (nausea)
            playerRig.localPosition = basePos + shake;
            yield return new WaitForSeconds(0.03f);
        }
        playerRig.localPosition = basePos;
    }

    private IEnumerator CameraShakeBurst(float intensity, float duration)
    {
        Vector3 basePos = playerRig.localPosition;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            Vector3 shake = Random.insideUnitSphere * intensity;
            shake.y *= 0.3f;
            playerRig.localPosition = basePos + shake;
            yield return null;
        }
        playerRig.localPosition = basePos;
    }

    // ============================================================
    // HELPERS
    // ============================================================

    private void DisableControllers()
    {
        if (moveProvider != null) moveProvider.enabled = false;
        if (turnProvider != null) turnProvider.enabled = false;
        if (snapTurnProvider != null) snapTurnProvider.enabled = false;
    }

    private void RotatePlayerToFaceMarker()
    {
        Vector3 dir = doorMarker.position - playerRig.position;
        dir.y = 0;
        dir.Normalize();

        if (dir.sqrMagnitude > 0.001f)
            playerRig.rotation = Quaternion.LookRotation(dir);
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
        Color red = new Color(0.8f, 0f, 0f, 0.7f);
        Color black = new Color(0f, 0f, 0f, 1f);
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