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
    [Tooltip("Leave ENABLED in hierarchy. Script disables it in Awake.")]
    [SerializeField] private GameObject monster;
    [SerializeField] private Animator monsterAnimator;

    [Header("Animator State Names")]
    [SerializeField] private string chaseStateName = "Chase";
    [SerializeField] private string attackStateName = "Attack";

    [Header("Cross-scene references - auto-found by name")]
    [Tooltip("Name of the empty/cube you placed where the monster should spawn from.")]
    [SerializeField] private string doorMarkerName = "DoorMarker";
    [Tooltip("Tag of your player rig.")]
    [SerializeField] private string playerTag = "Player";

    [Header("Audio")]
    [SerializeField] private AudioSource crashOrCrySound;
    [SerializeField] private AudioSource revealStinger;
    [SerializeField] private AudioSource attackScreech;

    [Header("Sequence Timing")]
    [SerializeField] private float freezeDuration = 2f;
    [SerializeField] private float fadeOutDuration = 1f;
    [SerializeField] private float blackHoldDuration = 0.8f;
    [SerializeField] private float fadeInDuration = 0.4f;
    [SerializeField] private float startChaseSpeed = 6f;
    [SerializeField] private float maxChaseSpeed = 10f;
    [SerializeField] private float chaseAcceleration = 5f;
    [SerializeField] private float attackDistance = 1.5f;
    [SerializeField] private float maxChaseTime = 4f;
    [SerializeField] private float attackHoldDuration = 0.6f;

    [Header("Ground Snapping (chase only)")]
    [SerializeField] private LayerMask groundMask = ~0;
    [SerializeField] private float groundRayStartHeight = 2f;
    [SerializeField] private float groundRayDistance = 10f;

    [Header("SEATED PLAYER - Attack Position")]
    [Tooltip("How far in front of the player the monster stops to attack.")]
    [SerializeField] private float attackStopDistance = 1.0f;
    [Tooltip("FINE-TUNE: Negative = monster lower, positive = higher. For seated VR try between -0.5 and -0.2.")]
    [SerializeField] private float seatedHeightOffset = -0.3f;
    [Tooltip("Estimated monster height. Only used if auto-detection fails.")]
    [SerializeField] private float fallbackMonsterHeight = 2f;

    [Header("Horror Atmosphere")]
    [Tooltip("Scenes whose lights get turned off during the scare.")]
    [SerializeField] private string[] scenesToDarken = { "final_jumpscare", "Second Area" };
    [Tooltip("Keep this spotlight enabled during the scare (lights the monster).")]
    [SerializeField] private Light monsterSpotlight;

    [Header("Impact Effects")]
    [SerializeField] private float redFlashDuration = 0.3f;
    [SerializeField] private float holdOnBlackDuration = 3f;
    [SerializeField] private Image fadeImage;

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

    private void Awake()
    {
        Debug.LogError("[WheelchairFinale] Awake on " + gameObject.name);

        if (monster != null)
        {
            monster.SetActive(false);

            // Measure monster height while it's active (do this before disabling)
            MeasureMonsterHeight();
        }

        // Make sure the monster spotlight is off until needed
        if (monsterSpotlight != null) monsterSpotlight.enabled = false;
    }

    private void MeasureMonsterHeight()
    {
        if (monster == null) return;

        // Get the tallest renderer bounds
        Renderer[] renderers = monster.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            Bounds combined = renderers[0].bounds;
            foreach (Renderer r in renderers) combined.Encapsulate(r.bounds);
            monsterHeight = combined.size.y;
            Debug.LogError("[WheelchairFinale] Measured monster height: " + monsterHeight);
        }
        else
        {
            monsterHeight = fallbackMonsterHeight;
        }
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
        // Camera
        if (Camera.main != null)
            playerCamera = Camera.main.transform;

        // Player rig + locomotion
        GameObject playerObj = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObj != null)
        {
            playerRig = playerObj.transform;
            moveProvider = playerObj.GetComponentInChildren<ContinuousMoveProvider>();
            turnProvider = playerObj.GetComponentInChildren<ContinuousTurnProvider>();
            snapTurnProvider = playerObj.GetComponentInChildren<SnapTurnProvider>();
        }

        // Door marker
        GameObject markerObj = GameObject.Find(doorMarkerName);
        if (markerObj != null) doorMarker = markerObj.transform;

        Debug.LogError("[WheelchairFinale] refs: cam=" + (playerCamera != null) +
                       " rig=" + (playerRig != null) + " marker=" + (doorMarker != null));
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

        // === PHASE 1: FREEZE PLAYER ===
        Debug.LogError("[Phase 1] Freeze");
        DisableControllers();
        if (crashOrCrySound != null) crashOrCrySound.Play();

        // === PHASE 2: FROZEN SILENCE ===
        yield return new WaitForSeconds(freezeDuration);

        // === PHASE 3: FADE TO BLACK ===
        Debug.LogError("[Phase 3] Fade out");
        EnsureFadeImageExists();
        yield return StartCoroutine(FadeColor(new Color(0, 0, 0, 0), new Color(0, 0, 0, 1), fadeOutDuration));

        // === PHASE 4: REORIENT IN DARKNESS ===
        Debug.LogError("[Phase 4] Rotate player, darken world, position monster");
        RotatePlayerToFaceMarker();
        DarkenWorld();

        // Position monster EXACTLY at the DoorMarker
        monster.transform.position = doorMarker.position;
        Debug.LogError("[WheelchairFinale] Monster placed at DoorMarker: " + doorMarker.position);
        FacePlayerHorizontal();
        monster.SetActive(true);

        // Enable monster spotlight if set
        if (monsterSpotlight != null) monsterSpotlight.enabled = true;

        // Force the Animator to NOT use root motion so our transform.position wins
        monsterAnimator.applyRootMotion = false;
        monsterAnimator.Play(chaseStateName, 0, 0f);

        if (revealStinger != null) revealStinger.Play();

        // === PHASE 5: BRIEF HOLD ===
        yield return new WaitForSeconds(blackHoldDuration);

        // === PHASE 6: FADE IN - monster is visible charging ===
        Debug.LogError("[Phase 6] Fade in");
        yield return StartCoroutine(FadeColor(new Color(0, 0, 0, 1), new Color(0, 0, 0, 0), fadeInDuration));

        // === PHASE 7: CHASE ===
        Debug.LogError("[Phase 7] Chase");
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

            // Ground snap (only during chase, not spawn)
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

        // === PHASE 8: POSITION FOR SEATED ATTACK ===
        Debug.LogError("[Phase 8] Position for seated attack");

        // Direction from monster to player (horizontal only)
        Vector3 dirToPlayer = (playerCamera.position - monster.transform.position);
        dirToPlayer.y = 0;
        dirToPlayer.Normalize();

        // Place monster in front of player at attackStopDistance
        Vector3 attackPos = playerCamera.position - dirToPlayer * attackStopDistance;

        // CRITICAL for seated VR: position monster so its HEAD is at camera height
        // Monster pivot is at its feet, so lower it by 70% of its height + offset
        attackPos.y = playerCamera.position.y - (monsterHeight * 0.7f) + seatedHeightOffset;

        monster.transform.position = attackPos;
        FacePlayerHorizontal();

        Debug.LogError("[WheelchairFinale] Attack position: " + attackPos +
                       " | Camera at: " + playerCamera.position +
                       " | Monster height: " + monsterHeight);

        // === PHASE 9: ATTACK + RED FLASH + LUNGE ===
        Debug.LogError("[Phase 9] ATTACK");
        monsterAnimator.Play(attackStateName, 0, 0f);
        if (attackScreech != null) attackScreech.Play();

        yield return StartCoroutine(RedFlashAndFillVision());

        // === PHASE 10: HOLD ON BLACK ===
        yield return new WaitForSeconds(holdOnBlackDuration);

        OnFinaleComplete();
    }

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
                    if (l == monsterSpotlight) continue; // keep spotlight on
                    if (!l.enabled) continue;

                    l.enabled = false;
                    disabledLights.Add(l);
                }
            }
        }

        // Cache current ambient
        cachedAmbientMode = RenderSettings.ambientMode;
        cachedAmbientLight = RenderSettings.ambientLight;
        cachedAmbientIntensity = RenderSettings.ambientIntensity;
        cachedReflectionIntensity = RenderSettings.reflectionIntensity;

        // Drop to near-black
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.05f, 0.02f, 0.05f);
        RenderSettings.ambientIntensity = 0.1f;
        RenderSettings.reflectionIntensity = 0.1f;

        Debug.LogError("[WheelchairFinale] Darkened world - disabled " + disabledLights.Count + " lights");
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
        EnsureFadeImageExists();

        Color red = new Color(0.8f, 0f, 0f, 0.7f);
        Color black = new Color(0f, 0f, 0f, 1f);
        fadeImage.color = red;

        // Lunge monster toward camera while flash fades to black
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