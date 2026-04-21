using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class WheelchairFinale : MonoBehaviour
{
    [Header("Monster")]
    [Tooltip("Leave ENABLED in hierarchy. Script disables it in Awake.")]
    [SerializeField] private GameObject monster;
    [SerializeField] private Animator monsterAnimator;

    [Header("Animator State Names")]
    [SerializeField] private string chaseStateName = "Chase";
    [SerializeField] private string attackStateName = "Attack";

    [Header("Audio")]
    [Tooltip("The audio that plays BEHIND the player to make them turn around. Spatial 3D.")]
    [SerializeField] private AudioSource lurePhaseSound;
    [Tooltip("The moment of reveal - plays when the monster materialises in front of them.")]
    [SerializeField] private AudioSource revealStinger;
    [Tooltip("The final attack screech.")]
    [SerializeField] private AudioSource attackScreech;

    [Header("Sequence Timing")]
    [Tooltip("Distance BEHIND the player the lure sound plays from.")]
    [SerializeField] private float lureSoundDistance = 5f;
    [Tooltip("How long the lure sound plays so player has time to turn around.")]
    [SerializeField] private float lureDuration = 3f;
    [Tooltip("How far away the monster appears when it materialises in front.")]
    [SerializeField] private float monsterRevealDistance = 10f;
    [Tooltip("Brief moment of silence before charge starts. Dread.")]
    [SerializeField] private float silenceBeforeCharge = 0.4f;
    [Tooltip("Starting chase speed.")]
    [SerializeField] private float startChaseSpeed = 8f;
    [Tooltip("Maximum chase speed (accelerates up to this).")]
    [SerializeField] private float maxChaseSpeed = 14f;
    [Tooltip("How fast the monster accelerates.")]
    [SerializeField] private float chaseAcceleration = 6f;
    [Tooltip("Monster triggers attack when this close.")]
    [SerializeField] private float attackDistance = 1.3f;
    [Tooltip("Safety cap.")]
    [SerializeField] private float maxChaseTime = 4f;

    [Header("Ground Snapping")]
    [SerializeField] private LayerMask groundMask = ~0;
    [SerializeField] private float groundRayStartHeight = 2f;
    [SerializeField] private float groundRayDistance = 10f;

    [Header("Impact Effects")]
    [Tooltip("How long the red flash lasts before cut to black.")]
    [SerializeField] private float redFlashDuration = 0.3f;
    [Tooltip("Hold on the black screen.")]
    [SerializeField] private float holdOnBlackDuration = 3f;
    [Tooltip("Optional: existing full-screen Image. If empty, one is created.")]
    [SerializeField] private Image fadeImage;

    private Transform playerCamera;
    private bool hasTriggered = false;
    private GameObject lureSoundObject;

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

    private Transform FindPlayerCamera()
    {
        if (Camera.main != null) return Camera.main.transform;
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) return p.transform;
        return null;
    }

    private IEnumerator PlayScare()
    {
        playerCamera = FindPlayerCamera();

        if (monster == null || monsterAnimator == null || playerCamera == null)
        {
            Debug.LogError("[WheelchairFinale] refs missing: monster=" + (monster != null) +
                           " anim=" + (monsterAnimator != null) + " cam=" + (playerCamera != null));
            yield break;
        }

        // === PHASE 1: THE LURE - play sound BEHIND the player ===
        Debug.LogError("[WheelchairFinale] Phase 1: Lure sound behind player");
        PlayLureSoundBehindPlayer();

        // Let the player hear it and turn around
        yield return new WaitForSeconds(lureDuration);

        // Stop the lure
        if (lurePhaseSound != null && lurePhaseSound.isPlaying) lurePhaseSound.Stop();

        // === PHASE 2: BRIEF SILENCE - dread ===
        Debug.LogError("[WheelchairFinale] Phase 2: Silence");
        yield return new WaitForSeconds(silenceBeforeCharge);

        // === PHASE 3: MATERIALISE - monster appears in front of wherever they're looking ===
        Debug.LogError("[WheelchairFinale] Phase 3: Materialise in front of player's gaze");
        SpawnMonsterInFrontOfGaze();
        monster.SetActive(true);
        yield return null; // let it register

        // Play reveal stinger
        if (revealStinger != null) revealStinger.Play();
        monsterAnimator.Play(chaseStateName, 0, 0f);

        // === PHASE 4: THE CHASE - fast with acceleration ===
        Debug.LogError("[WheelchairFinale] Phase 4: Chase");
        float currentSpeed = startChaseSpeed;
        float timer = 0f;

        while (timer < maxChaseTime)
        {
            // Accelerate speed
            currentSpeed = Mathf.Min(currentSpeed + chaseAcceleration * Time.deltaTime, maxChaseSpeed);

            // Target is player's XZ
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

        // === PHASE 5: ATTACK + RED FLASH + MONSTER FILLS VISION ===
        Debug.LogError("[WheelchairFinale] Phase 5: ATTACK");
        monsterAnimator.Play(attackStateName, 0, 0f);
        if (attackScreech != null) attackScreech.Play();

        // Red flash + monster lunges into camera simultaneously
        yield return StartCoroutine(RedFlashAndFillVision());

        // === PHASE 6: HOLD ON BLACK ===
        yield return new WaitForSeconds(holdOnBlackDuration);

        OnFinaleComplete();
    }

    private void PlayLureSoundBehindPlayer()
    {
        if (lurePhaseSound == null) return;

        // Create a temp GameObject so we can position the sound source behind the player
        // This way the spatial 3D audio comes from BEHIND
        lureSoundObject = new GameObject("LureSoundPosition");
        Vector3 behindDir = -playerCamera.forward;
        behindDir.y = 0;
        behindDir.Normalize();
        lureSoundObject.transform.position = playerCamera.position + behindDir * lureSoundDistance;

        // Move the AudioSource to this position (temporarily)
        lurePhaseSound.transform.position = lureSoundObject.transform.position;
        lurePhaseSound.spatialBlend = 1f; // Full 3D
        lurePhaseSound.Play();
    }

    private void SpawnMonsterInFrontOfGaze()
    {
        // Wherever the player is looking NOW, spawn the monster there
        Vector3 forwardDir = playerCamera.forward;
        forwardDir.y = 0;
        forwardDir.Normalize();

        Vector3 spawnPos = playerCamera.position + forwardDir * monsterRevealDistance;

        // Ground snap the spawn
        if (Physics.Raycast(spawnPos + Vector3.up * groundRayStartHeight,
                            Vector3.down,
                            out RaycastHit hit,
                            groundRayDistance * 2f,
                            groundMask,
                            QueryTriggerInteraction.Ignore))
        {
            spawnPos.y = hit.point.y;
        }
        else
        {
            spawnPos.y = playerCamera.position.y - 1.5f;
        }

        monster.transform.position = spawnPos;
        FacePlayerHorizontal();
    }

    private void FacePlayerHorizontal()
    {
        Vector3 lookTarget = new Vector3(playerCamera.position.x,
                                         monster.transform.position.y,
                                         playerCamera.position.z);
        monster.transform.LookAt(lookTarget);
    }

    private IEnumerator RedFlashAndFillVision()
    {
        EnsureFadeImageExists();

        // Phase A: Red flash (bright red, instant)
        Color red = new Color(0.8f, 0f, 0f, 0.7f);
        Color black = new Color(0f, 0f, 0f, 1f);
        Color transparent = new Color(0f, 0f, 0f, 0f);

        fadeImage.color = red;

        // Animate monster filling vision - move it TO the camera while red flash holds
        Vector3 monsterStart = monster.transform.position;
        Vector3 cameraPos = playerCamera.position;
        float elapsed = 0f;

        while (elapsed < redFlashDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / redFlashDuration;

            // Lunge monster toward the camera's face
            monster.transform.position = Vector3.Lerp(monsterStart, cameraPos, t);
            monster.transform.LookAt(playerCamera.position);

            // Transition red → black over the duration
            fadeImage.color = Color.Lerp(red, black, t);

            yield return null;
        }

        // Cut to full black
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
        // Hook up whatever comes next:
        // UnityEngine.SceneManagement.SceneManager.LoadScene("Credits");
    }
}