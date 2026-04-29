using System.Collections;
using UnityEngine;

public class ToiletDoor : MonoBehaviour
{
    public bool openPadlock = false;
    private bool open = false;
    public float duration = 3.0f;
    public float slamDuration = 0.15f;
    public float DoorOpenAngle = 120.0f;
    public AudioSource flushAudio;
    public AudioClip slamClip;
    public AudioSource roomAmbienceAudio;
    public AudioSource bgMusic;

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
        StartCoroutine(FadeInAmbience(5f));
        flushAudio.Play();
        if (bgMusic != null) bgMusic.Stop();
    }

    public void SlamDoor(Collider triggerCollider)
    {
        triggerCollider.enabled = false;
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
            transform.rotation = Quaternion.Slerp(startRot, endRot, elapsed / duration);
            yield return null;
        }
        transform.rotation = endRot;
    }

    IEnumerator closeDoor()
    {
        float elapsed = 0f;
        Quaternion startRot = transform.rotation;
        Quaternion endRot = Quaternion.Euler(defaulRot);
        while (elapsed < slamDuration)
        {
            elapsed += Time.deltaTime;
            transform.rotation = Quaternion.Slerp(startRot, endRot, elapsed / slamDuration);
            yield return null;
        }
        transform.rotation = endRot;
        if (slamClip != null)
            AudioSource.PlayClipAtPoint(slamClip, transform.position, 1f);
    }

    public void OpenAfterDelay(float delay)
    {
        StartCoroutine(DelayedOpen(delay));
    }

    IEnumerator DelayedOpen(float delay)
    {
        yield return new WaitForSeconds(delay);
        StartCoroutine(openDoor());
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