using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class TutorialDoor : MonoBehaviour
{
    private bool open = false;

    public float smooth = 3.0f;
    public float DoorOpenAngle = 120.0f;
    public float duration = 3.0f;


    public AudioSource audioSource;
    private Vector3 defaulRot;
    private Vector3 openRot;

    void Start()
    {
        defaulRot = transform.eulerAngles;
        openRot = new Vector3(defaulRot.x, defaulRot.y + DoorOpenAngle, defaulRot.z);
    }


    public void unlockLock()
    {
        open = true;

        // Re-enable oxygen and toxicity when tutorial ends
        OxygenManager.Instance.disabled = false;
        ToxicityDevice.Instance.disabled = false;


        audioSource.Play();
        StartCoroutine(openDoor());

        
    }

    public IEnumerator openDoor()
    {
        float elapsed = 0f;

        Quaternion startRot = Quaternion.Euler(defaulRot);
        Quaternion endRot = Quaternion.Euler(openRot);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            
            // Using Quaternions is safer for rotations to avoid "Gimbal Lock"
            transform.rotation = Quaternion.Slerp(startRot, endRot, elapsed);

            // Wait for the next frame
            yield return null;
        }

        // Force the final rotation to be exact at the end
        transform.rotation = endRot;
    }
}