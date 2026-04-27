using UnityEngine;

public class OxygenManager : MonoBehaviour
{
    public static OxygenManager Instance { get; private set; }
    void Awake() { Instance = this; }

    public BreathInputML breathInput;
    public float drainRate = 5f;
    public float refillRate = 25f;

    [Header("Audio")]
    public AudioSource audioSource;

    private bool canPlayBreathAudio = false;

    void Update()
    {
        if (OxygenTank.Instance == null) return;

        if (OxygenTank.Instance.isRefilling)
        {
            OxygenTank.Instance.oxygenLevel = Mathf.Clamp(
                OxygenTank.Instance.oxygenLevel + refillRate * Time.deltaTime, 0f, 100f);
            canPlayBreathAudio = true; // prime for next breath
            return;
        }

        if (breathInput != null && breathInput.isBreathing)
        {
            OxygenTank.Instance.oxygenLevel = Mathf.Clamp(
                OxygenTank.Instance.oxygenLevel - drainRate * Time.deltaTime, 0f, 100f);

            if (canPlayBreathAudio)
            {
                if (audioSource != null)
                {
                    audioSource.loop = false;
                    audioSource.Play();
                }
                canPlayBreathAudio = false;
            }
        }
        else
        {
            // stop audio if it somehow keeps playing
            if (audioSource != null && audioSource.isPlaying)
                audioSource.Stop();
        }
    }
}