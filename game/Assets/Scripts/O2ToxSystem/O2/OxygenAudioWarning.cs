using UnityEngine;

public class OxygenAudioWarning : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip warning75;
    [SerializeField] private AudioClip warning50;
    [SerializeField] private AudioClip warning25;
    [SerializeField] private AudioClip warning0;

    void OnEnable() { OxygenTank.OnOxygenThresholdCrossed += HandleThreshold; }
    void OnDisable() { OxygenTank.OnOxygenThresholdCrossed -= HandleThreshold; }

    private void HandleThreshold(int threshold)
    {
        AudioClip clip = threshold switch
        {
            75 => warning75,
            50 => warning50,
            25 => warning25,
            0 => warning0,
            _ => null
        };

        if (clip != null && audioSource != null)
            audioSource.PlayOneShot(clip);
    }
}