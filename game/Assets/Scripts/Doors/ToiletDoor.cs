using System.Collections;
using UnityEngine;

public class ToiletDoor : MonoBehaviour
{
    public bool openPadlock = false;
    private bool open = false;
    public float duration = 3.0f;
    public float DoorOpenAngle = 120.0f;
    public AudioSource flushAudio;
    public AudioSource slamAudio;
    public AudioSource roomAmbienceAudio;

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
        StartCoroutine(FadeInAmbience(5f)); // fades in over 5 seconds as door opens
        flushAudio.Play();
    }

    public void SlamDoor()
    {
        StartCoroutine(closeDoor());
    }

    IEnumerator openDoor()
    {
        float elapsed = 0f;
        Quaternion startRot = Quaternion.Euler(defaulRot);
        Quaternion endRot = Quaternion.Euler(openRot);
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.rotation = Quaternion.Slerp(startRot, endRot, elapsed);
            yield return null;
        }
        transform.rotation = endRot;
    }

    IEnumerator closeDoor()
    {
        float elapsed = 0f;
        Quaternion startRot = transform.rotation;
        Quaternion endRot = Quaternion.Euler(defaulRot);
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.rotation = Quaternion.Slerp(startRot, endRot, elapsed);
            yield return null;
        }
        transform.rotation = endRot;
        if (slamAudio != null) slamAudio.Play(); // plays when fully shut
    }

    IEnumerator FadeInAmbience(float fadeDuration)
    {
        roomAmbienceAudio.volume = 0f;
        roomAmbienceAudio.Play();
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            roomAmbienceAudio.volume = Mathf.Clamp01(elapsed / fadeDuration);
            yield return null;
        }
        roomAmbienceAudio.volume = 1f;
    }
}