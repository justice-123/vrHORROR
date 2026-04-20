using System.Collections;
using UnityEngine;

public class ToiletDoor : MonoBehaviour
{
    
    public bool openPadlock = false;
    private bool open = false;
    public float duration = 3.0f;
    public float DoorOpenAngle = 120.0f;
    public AudioSource flushAudio;

    private Vector3 defaulRot;
    private Vector3 openRot;
    

    void Start()
    {
        openPadlock = false;
        defaulRot = transform.eulerAngles;
        openRot = new Vector3(defaulRot.x, defaulRot.y + DoorOpenAngle, defaulRot.z);
    }

    public void PadlockOpen()
    {
        openPadlock = true;
        StartCoroutine(openDoor());
        flushAudio.Play();
    }

    IEnumerator openDoor()
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

        flushAudio.Play();
    }

}
