using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class VRDoorOpenSound : MonoBehaviour
{
    [Header("Audio")]
    public AudioClip openDoorClip;

    [Header("Settings")]
    public bool playOnlyOnce = true;

    private AudioSource audioSource;
    private bool hasPlayed = false;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (playOnlyOnce && hasPlayed)
            return;

        if (openDoorClip != null)
        {
            audioSource.PlayOneShot(openDoorClip);
            hasPlayed = true;
        }
        else
        {
            Debug.LogWarning("Missing openDoorClip on " + gameObject.name);
        }
    }
}