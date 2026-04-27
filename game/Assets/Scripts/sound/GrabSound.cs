using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class GrabSound : MonoBehaviour
{
    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f; // 3D sound
        audioSource.volume = 1f;
        audioSource.minDistance = 1f;
        audioSource.maxDistance = 8f;
    }

    public void PlayGrabSound()
    {
        if (audioSource.clip != null)
        {
            audioSource.PlayOneShot(audioSource.clip);
        }
        else
        {
            Debug.LogWarning("Missing grab sound clip.", this);
        }
    }
}