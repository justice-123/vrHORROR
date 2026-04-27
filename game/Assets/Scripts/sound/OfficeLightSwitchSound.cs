using UnityEngine;

[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(BoxCollider))]
public class OfficeLightSwitchSound : MonoBehaviour
{
    [Header("Audio Clip")]
    public AudioClip switchSound;

    [Header("Settings")]
    public bool playOnlyOnce = true;

    private AudioSource audioSource;
    private bool hasTriggered = false;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f; // 3D sound
        audioSource.volume = 100f;
        audioSource.minDistance = 0f;
        audioSource.maxDistance = 100f;

        BoxCollider box = GetComponent<BoxCollider>();
        box.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (playOnlyOnce && hasTriggered)
            return;

        PlaySwitchSound();
    }

    private void PlaySwitchSound()
    {
        if (switchSound != null)
        {
            audioSource.PlayOneShot(switchSound);
        }

        hasTriggered = true;
    }
}