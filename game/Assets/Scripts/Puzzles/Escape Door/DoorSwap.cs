using UnityEngine;

public class DoorSwap : MonoBehaviour
{
    public GameObject brokenObject;

    [Header("Audio")]
    public AudioClip breakSound;
    public float volume = 1f;

    public void swapToBrokenModel()
    {
        if (breakSound != null)
        {
            AudioSource.PlayClipAtPoint(breakSound, transform.position, volume);
        }

        if (brokenObject != null)
        {
            brokenObject.SetActive(true);
        }

        gameObject.SetActive(false);
    }
}