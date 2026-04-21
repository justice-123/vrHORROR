using System.Collections;
using UnityEngine;
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

    [Header("Cross-scene references - leave empty, auto-found at runtime")]
    [Tooltip("Name of the door GameObject in the scene. Must match exactly.")]
    [SerializeField] private string doorGameObjectName = "Front Door";
    [Tooltip("Tag of your player rig. Default is 'Player'.")]
    [SerializeField] private string playerTag = "Player";

    // These get found at runtime
    private Transform doorPosition;
    private Transform playerRig;
    private ContinuousMoveProvider moveProvider;
    private ContinuousTurnProvider turnProvider;
    private SnapTurnProvider snapTurnProvider;

    [Header("Audio")]
    [SerializeField] private AudioSource crashOrCrySound;
    [SerializeField] private AudioSource revealStinger;
    [SerializeField] private AudioSource attackScreech;

    [Header("Sequence Timing")]
    [SerializeField] private float freezeDuration = 2f;
    [SerializeField] private float fadeOutDuration = 1f;
    [SerializeField] private float blackHoldDuration = 0.8f;
    [SerializeField] private float fadeInDuration = 0.4f;
    [SerializeField] private float startChaseSpeed = 8f;
    [SerializeField] private float maxChaseSpeed = 14f;
    [SerializeField] private float chaseAcceleration = 6f;
    [SerializeField] private float attackDistance = 1.5f;
    [SerializeField] private float maxChaseTime = 4f;

    [Header("Ground Snapping")]
    [SerializeField] private LayerMask groundMask = ~0;
    [SerializeField] private float groundRayStartHeight = 2f;
    [SerializeField] private float groundRayDistance = 10f;
    [Tooltip("Vertical offset so monster stops at camera height for attack, not above or below.")]
    [SerializeField] private float attackHeightOffset = 0f;

    [Header("Impact Effects")]
    [SerializeField] private float redFlashDuration = 0.3f;
    [SerializeField] private float holdOnBlackDuration = 3f;
    [SerializeField] private Image fadeImage;

    private Transform playerCamera;
    private bool hasTriggered = false;

    private void Awake()
    {
        Debug.LogError("[WheelchairFinale] Awake on " + gameObject.name);
        if (monster != null) monster.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered) return;
        if (!other.CompareTag("Player")) return;

        Debug.LogError("[WheelchairFinale] trigger hit by Player — starting scare");
        hasTriggered = true;
        StartCoroutine(PlayScare());
    }

    private void FindCrossSceneReferences()
    {
        // Find player rig by tag
        GameObject playerObj = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObj != null)
        {
            playerRig = playerObj.transform;
            Debug.LogError("[WheelchairFinale] found player rig: " + playerRig.name);

            // Find locomotion providers on the player hierarchy
            moveProvider = playerObj.GetComponentInChildren<ContinuousMoveProvider>();
            turnProvider = playerObj.GetComponentInChildren<ContinuousTurnProvider>();
            snapTurnProvider = playerObj.GetComponentInChildren<SnapTurnProvider>();
        }
        else
        {
            Debug.LogError("[WheelchairFinale] no GameObject with tag '" + playerTag + "' found!");
        }

        // Find door by name
        GameObject doorObj = GameObject.Find(doorGameObjectName);
        if (doorObj != null)
        {
            doorPosition = doorObj.transform;
            Debug.LogError("[WheelchairFinale] found door: " + doorPosition.name);
        }
        else
        {
            Debug.LogError("[WheelchairFinale] no GameObject named '" + doorGameObjectName + "' found!");
        }
    }

    private Transform FindPlayerCamera()
    {
        if (Camera.main != null) return Camera.main.transform;
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) return p.transform;
        return null;
    }

    private IEnumerator PlayScare()
    {
        // Auto-find all cross-scene references
        playerCamera = FindPlayerCamera();
        FindCrossSceneReferences();

        if (monster == null || monsterAnimator == null || playerCamera == null || doorPosition == null || playerRig == null)
        {
            Debug.LogError("[WheelchairFinale] refs missing: monster=" + (monster != null) +
                           " anim=" + (monsterAnimator != null) +
                           " cam=" + (playerCamera != null) +
                           " door=" + (doorPosition != null) +
                           " rig=" + (playerRig != null));
            yield break;
        }

        // === PHASE 1: FREEZE PLAYER ===
        Debug.LogError("[WheelchairFinale] Phase 1: Freeze player");
        DisableControllers();
        if (crashOrCrySound != null) crashOrCrySound.Play();

        // === PHASE 2: 2 SECONDS FROZEN SILENCE ===
        yield return new WaitForSeconds(freezeDuration);

        // === PHASE 3: FADE TO BLACK ===
        Debug.LogError("[WheelchairFinale] Phase 3: Fade to black");
        EnsureFadeImageExists();
        yield return StartCoroutine(FadeColor(
            new Color(0, 0, 0, 0),
            new Color(0, 0, 0, 1),
            fadeOutDuration));

        // === PHASE 4: ROTATE PLAYER TO FACE DOOR (in darkness) ===
        Debug.LogError("[WheelchairFinale] Phase 4: Rotate player to face door");
        RotatePlayerToFaceDoor();

        // Place monster at the door, hidden behind black screen
        monster.transform.position = doorPosition.position;
        Debug.LogError("[WheelchairFinale] Monster spawned at: " + monster.transform.position + " DoorMarker at: " + doorPosition.position);
        FacePlayerHorizontal();
        monster.SetActive(true);
        monsterAnimator.Play(chaseStateName, 0, 0f);
        if (revealStinger != null) revealStinger.Play();

        // === PHASE 5: HOLD ON BLACK BRIEFLY ===
        yield return new WaitForSeconds(blackHoldDuration);

        // === PHASE 6: FADE IN - monster is already charging ===
        Debug.LogError("[WheelchairFinale] Phase 6: Fade back in");
        yield return StartCoroutine(FadeColor(
            new Color(0, 0, 0, 1),
            new Color(0, 0, 0, 0),
            fadeInDuration));

        // === PHASE 7: THE CHASE ===
        Debug.LogError("[WheelchairFinale] Phase 7: Chase");
        float currentSpeed = startChaseSpeed;
        float timer = 0f;

        while (timer < maxChaseTime)
        {
            currentSpeed = Mathf.Min(currentSpeed + chaseAcceleration * Time.deltaTime, maxChaseSpeed);

            Vector3 horizontalTarget = new Vector3(playerCamera.position.x,
                                                   monster.transform.position.y,
                                                   playerCamera.position.z);

            float horizontalDistance = Vector2.Distance(
                new Vector2(monster.transform.position.x, monster.transform.position.z),
                new Vector2(playerCamera.position.x, playerCamera.position.z));

            if (horizontalDistance <= attackDistance) break;

            Vector3 nextPos = Vector3.MoveTowards(monster.transform.position,
                                                  horizontalTarget,
                                                  currentSpeed * Time.deltaTime);

            // Ground snap
            if (Physics.Raycast(nextPos + Vector3.up * groundRayStartHeight,
                                Vector3.down,
                                out RaycastHit hit,
                                groundRayDistance,
                                groundMask,
                                QueryTriggerInteraction.Ignore))
            {
                nextPos.y = hit.point.y;
            }

            monster.transform.position = nextPos;
            FacePlayerHorizontal();

            timer += Time.deltaTime;
            yield return null;
        }

        // === PHASE 8: STOP AT CORRECT HEIGHT BEFORE ATTACKING ===
        Debug.LogError("[WheelchairFinale] Phase 8: Position for attack");
        // Bring monster to camera height so attack plays in your face, not above
        Vector3 attackPos = monster.transform.position;
        attackPos.y = playerCamera.position.y + attackHeightOffset;

        // Also pull it in close
        Vector3 dirToPlayer = (playerCamera.position - monster.transform.position);
        dirToPlayer.y = 0;
        dirToPlayer.Normalize();
        attackPos = playerCamera.position - dirToPlayer * 0.8f; // Just in front of camera
        attackPos.y = playerCamera.position.y + attackHeightOffset;
        monster.transform.position = attackPos;
        FacePlayerHorizontal();

        // === PHASE 9: ATTACK + RED FLASH ===
        Debug.LogError("[WheelchairFinale] Phase 9: ATTACK");
        monsterAnimator.Play(attackStateName, 0, 0f);
        if (attackScreech != null) attackScreech.Play();

        yield return StartCoroutine(RedFlashAndFillVision());

        // === PHASE 10: HOLD ON BLACK ===
        yield return new WaitForSeconds(holdOnBlackDuration);

        OnFinaleComplete();
    }

    private void DisableControllers()
    {
        Debug.LogError("[WheelchairFinale] Disabling controllers");
        if (moveProvider != null) moveProvider.enabled = false;
        if (turnProvider != null) turnProvider.enabled = false;
        if (snapTurnProvider != null) snapTurnProvider.enabled = false;
    }

    private void RotatePlayerToFaceDoor()
    {
        if (playerRig == null) return;

        // Calculate direction from player to door
        Vector3 dirToDoor = doorPosition.position - playerRig.position;
        dirToDoor.y = 0;
        dirToDoor.Normalize();

        // Rotate the rig to face that direction
        Quaternion targetRot = Quaternion.LookRotation(dirToDoor);
        playerRig.rotation = targetRot;
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
        EnsureFadeImageExists();

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