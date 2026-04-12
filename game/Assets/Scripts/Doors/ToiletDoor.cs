using System.Collections;
using UnityEngine;

public class ToiletDoor : MonoBehaviour
{
    
    public bool beenInNursery = false;
    private bool open = false;
    public float duration = 3.0f;
    public float DoorOpenAngle = 120.0f;
    public AudioSource audioSource;

    private Vector3 defaulRot;
    private Vector3 openRot;
    

    void Start()
    {
        beenInNursery = false;
        defaulRot = transform.eulerAngles;
        openRot = new Vector3(defaulRot.x, defaulRot.y + DoorOpenAngle, defaulRot.z);
    }

    public void visitNursery()
    {
        beenInNursery = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (beenInNursery && !open && other.tag == "Player")
        {
            open = true;
            StartCoroutine(openDoor());
            audioSource.Play();
        }
    }

    IEnumerator openDoor()
    {
        float elapsed = 0f;
        // We calculate a duration based on your 'smooth' variable
        // If smooth is 3, the door takes roughly 1 second to open fully.

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
