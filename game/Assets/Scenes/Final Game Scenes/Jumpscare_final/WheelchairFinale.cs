using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;

public class WheelchairFinale : MonoBehaviour
{
    private Transform wheelchairRig;
    private Transform playerCamera;
    private ContinuousMoveProvider moveProvider;
    private ContinuousTurnProvider turnProvider;
    private SnapTurnProvider snapTurnProvider;
    private GameObject insectoid;
    private Animator insectoidAnim;
    private Transform insectoidJawLeft;
    private Transform insectoidJawRight;
    private CanvasGroup blackScreen;
    private Image blackScreenImage;

    [Header("Audio - drag in")]
    public AudioSource gnarlyCrashSound;
    public AudioSource chargeSound;

    [Header("Timing")]
    public float fadeToBlackDuration = 1f;
    public float fadeBackInDuration = 0.5f;

    private bool hasTriggered = false;
    private bool isReady = false;

    void Awake()
    {
        Debug.LogWarning("🚨 WHEELCHAIRFINALE AWAKE RAN 🚨");
    }

    void Start()
    {
        Debug.LogWarning("🚨 WHEELCHAIRFINALE START RAN 🚨");

        string startupStatus = "";

        // Find the player camera
        Camera mainCam = Camera.main;
        if (mainCam != null) { playerCamera = mainCam.transform; startupStatus += "CAM_OK "; }
        else startupStatus += "CAM_FAIL ";

        // Find the player rig
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            wheelchairRig = player.transform;
            startupStatus += "PLAYER_OK ";

            moveProvider = player.GetComponentInChildren<ContinuousMoveProvider>();
            turnProvider = player.GetComponentInChildren<ContinuousTurnProvider>();
            snapTurnProvider = player.GetComponentInChildren<SnapTurnProvider>();

            if (moveProvider != null) startupStatus += "MOVE_OK ";
            if (turnProvider != null) startupStatus += "TURN_OK ";
            if (snapTurnProvider != null) startupStatus += "SNAP_OK ";
        }
        else startupStatus += "PLAYER_FAIL ";

        // Find the Insectoid
        insectoid = GameObject.Find("Insectoid (1)");
        if (insectoid == null) insectoid = GameObject.Find("Insectoid");
        if (insectoid == null) insectoid = GameObject.Find("Insectoid(1)");

        if (insectoid != null)
        {
            startupStatus += "INSECT_OK ";
            insectoidAnim = insectoid.GetComponent<Animator>();
            if (insectoidAnim != null) startupStatus += "ANIM_OK ";
            else startupStatus += "ANIM_FAIL ";

            foreach (Transform child in insectoid.GetComponentsInChildren<Transform>())
            {
                string n = child.name.ToLower();
                if (n.Contains("jaw") && n.Contains("left")) insectoidJawLeft = child;
                if (n.Contains("jaw") && n.Contains("right")) insectoidJawRight = child;
            }
        }
        else startupStatus += "INSECT_FAIL ";

        // Find the black screen
        GameObject fadeObj = GameObject.Find("Fade Screen");
        if (fadeObj == null) fadeObj = GameObject.Find("BlackScreen");
        if (fadeObj == null) fadeObj = GameObject.Find("Black Screen");
        if (fadeObj == null) fadeObj = GameObject.Find("FadeScreen");

        if (fadeObj != null)
        {
            blackScreen = fadeObj.GetComponent<CanvasGroup>();
            blackScreenImage = fadeObj.GetComponent<Image>();
            if (blackScreen != null) startupStatus += "FADE_OK ";
            else startupStatus += "FADE_FAIL ";
        }
        else startupStatus += "FADE_FAIL ";

        // Check if ready
        if (playerCamera != null && wheelchairRig != null && insectoid != null && blackScreen != null)
        {
            isReady = true;
        }

        Debug.LogWarning("🚨 STARTUP STATUS: " + startupStatus + " | READY: " + isReady);

        // Flash colour for visual confirmation
        if (blackScreen != null)
        {
            StartCoroutine(StartupFlash());
        }
    }

    IEnumerator StartupFlash()
    {
        Color flashColor = isReady ? Color.yellow : Color.red;
        if (blackScreenImage != null) blackScreenImage.color = flashColor;
        blackScreen.alpha = 0.4f;
        yield return new WaitForSeconds(2f);
        blackScreen.alpha = 0f;
        if (blackScreenImage != null) blackScreenImage.color = Color.black;
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.LogWarning("🚨 TRIGGER HIT BY: " + other.name + " | TAG: " + other.tag);

        StartCoroutine(FlashColor(Color.blue, 0.5f, 1f));

        if (!hasTriggered && other.CompareTag("Player"))
        {
            if (!isReady)
            {
                Debug.LogWarning("🚨 NOT READY - SCARE ABORTED");
                StartCoroutine(FlashColor(Color.red, 0.5f, 2f));
                return;
            }

            hasTriggered = true;
            Debug.LogWarning("🚨 PLAYER DETECTED - STARTING SCARE");
            StartCoroutine(FlashColor(Color.green, 0.5f, 1f));
            StartCoroutine(ExecuteWheelchairScare());
        }
    }

    IEnumerator FlashColor(Color c, float alpha, float duration)
    {
        if (blackScreen == null || blackScreenImage == null) yield break;

        Color originalColor = blackScreenImage.color;
        blackScreenImage.color = c;
        blackScreen.alpha = alpha;
        yield return new WaitForSeconds(duration);
        blackScreen.alpha = 0f;
        blackScreenImage.color = originalColor;
    }

    IEnumerator ExecuteWheelchairScare()
    {
        Debug.LogWarning("🚨 PHASE 1: Freeze player");
        DisablePlayerMovement();
        if (gnarlyCrashSound != null) gnarlyCrashSound.Play();

        yield return new WaitForSeconds(2f);

        Debug.LogWarning("🚨 PHASE 2: Fade to black");
        if (blackScreenImage != null) blackScreenImage.color = Color.black;
        yield return StartCoroutine(FadeScreen(0f, 1f, fadeToBlackDuration));

        Debug.LogWarning("🚨 PHASE 3: Rotate player");
        Quaternion startRot = wheelchairRig.rotation;
        Quaternion endRot = startRot * Quaternion.Euler(0, 180f, 0);
        wheelchairRig.rotation = endRot;

        yield return new WaitForSeconds(0.8f);

        Debug.LogWarning("🚨 PHASE 4: Activate monster");
        insectoid.SetActive(true);
        if (insectoidAnim != null) insectoidAnim.SetTrigger("Run");

        yield return new WaitForSeconds(0.3f);

        Debug.LogWarning("🚨 PHASE 5: Fade back in");
        if (chargeSound != null) chargeSound.Play();
        yield return StartCoroutine(FadeScreen(1f, 0f, fadeBackInDuration));

        Debug.LogWarning("🚨 PHASE 6: Charge");
        float safetyTimer = 0f;
        while (Vector3.Distance(insectoid.transform.position, playerCamera.position) > 1.5f && safetyTimer < 5f)
        {
            insectoid.transform.position = Vector3.MoveTowards(
                insectoid.transform.position,
                playerCamera.position,
                8f * Time.deltaTime
            );
            insectoid.transform.LookAt(playerCamera.position);
            safetyTimer += Time.deltaTime;
            yield return null;
        }

        Debug.LogWarning("🚨 PHASE 7: Attack");
        if (insectoidAnim != null) insectoidAnim.SetTrigger("Attack");
        yield return StartCoroutine(FadeScreen(0f, 1f, 0.2f));

        Debug.LogWarning("🚨 SCARE COMPLETE");
    }

    void DisablePlayerMovement()
    {
        if (moveProvider != null) moveProvider.enabled = false;
        if (turnProvider != null) turnProvider.enabled = false;
        if (snapTurnProvider != null) snapTurnProvider.enabled = false;
    }

    IEnumerator FadeScreen(float startAlpha, float endAlpha, float duration)
    {
        if (blackScreen == null) yield break;

        float elapsed = 0f;
        blackScreen.alpha = startAlpha;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            blackScreen.alpha = Mathf.Lerp(startAlpha, endAlpha, elapsed / duration);
            yield return null;
        }
        blackScreen.alpha = endAlpha;
    }
}