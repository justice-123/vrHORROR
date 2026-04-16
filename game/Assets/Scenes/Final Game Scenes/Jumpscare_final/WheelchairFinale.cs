using System.Collections;
using UnityEngine;

public class WheelchairFinale : MonoBehaviour
{
    [Header("The Player")]
    public Transform wheelchairRig; // Drag the parent of the VR camera here
    public Transform playerCamera;

    [Header("The Trap")]
    public AudioSource gnarlyCrashSound; // The sound of the chair hitting the bottom
    public Transform leftDoor;
    public Transform rightDoor;
    public AudioSource doorBurstSound;

    [Header("The Insectoid")]
    public GameObject insectoid;
    public Animator insectoidAnim;
    public AudioSource chargeSound;
    public Transform insectoidJawLeft;  // <-- We replaced the single jaw with Left
    public Transform insectoidJawRight; // <-- And Right

    [Header("Cinematics")]
    public CanvasGroup blackScreen;

    private bool hasTriggered = false;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !hasTriggered)
        {
            hasTriggered = true;
            StartCoroutine(ExecuteWheelchairScare());
        }
    }

    IEnumerator ExecuteWheelchairScare()
    {
        // 1. THE CRASH & THE SPIN
        // The wheelchair hits the bottom of the ramp.
        gnarlyCrashSound.Play();

        // Spin the wheelchair 180 degrees to face back up the ramp over 1 second
        Quaternion startRot = wheelchairRig.rotation;
        Quaternion endRot = startRot * Quaternion.Euler(0, 180f, 0);
        float elapsed = 0f;
        while (elapsed < 1f)
        {
            elapsed += Time.deltaTime;
            wheelchairRig.rotation = Quaternion.Lerp(startRot, endRot, elapsed / 1f);
            yield return null;
        }

        yield return new WaitForSeconds(0.5f); // Half second of dizzy silence

        // 2. THE DOORS BURST OPEN
        doorBurstSound.Play();
        leftDoor.localRotation = Quaternion.Euler(0, -90f, 0); // Fling doors open
        rightDoor.localRotation = Quaternion.Euler(0, 90f, 0);

        // 3. THE CHARGE
        insectoid.SetActive(true);
        insectoidAnim.SetTrigger("Run");
        chargeSound.Play();

        // Run down the ramp
        while (Vector3.Distance(insectoid.transform.position, playerCamera.position) > 1.5f)
        {
            insectoid.transform.position = Vector3.MoveTowards(insectoid.transform.position, playerCamera.position, 8f * Time.deltaTime);
            insectoid.transform.LookAt(playerCamera.position);
            yield return null;
        }

        // 4. THE GROSS PAYOFF (The Double Mouth)
        insectoidAnim.SetTrigger("Attack");

        // Grab the normal size of both bones
        Vector3 normalLeftSize = insectoidJawLeft.localScale;
        Vector3 normalRightSize = insectoidJawRight.localScale;

        // Multiply them by 3 to make them massive
        Vector3 hugeLeftSize = normalLeftSize * 3f;
        Vector3 hugeRightSize = normalRightSize * 3f;

        elapsed = 0f;

        while (elapsed < 0.2f) // Happens incredibly fast right before the cut
        {
            elapsed += Time.deltaTime;
            float lerpFactor = elapsed / 0.2f;

            // Rapidly scale BOTH jaws at the exact same time
            insectoidJawLeft.localScale = Vector3.Lerp(normalLeftSize, hugeLeftSize, lerpFactor);
            insectoidJawRight.localScale = Vector3.Lerp(normalRightSize, hugeRightSize, lerpFactor);

            // Fade to black rapidly
            blackScreen.alpha = lerpFactor;
            yield return null;
        }

        // Game Over state here
    }
}