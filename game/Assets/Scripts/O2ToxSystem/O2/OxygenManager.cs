using UnityEngine;

public class OxygenManager : MonoBehaviour
{
    public static OxygenManager Instance { get; private set; }
    void Awake() { Instance = this; }

    public BreathInputML breathInput;
    public float drainRate = 5f;
    public float refillRate = 25f;
    public bool disabled = false;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioSource chokingAudio;

    private bool canPlayBreathAudio = false;
    private bool _deathTriggered = false;
    private bool _chokingTriggered = false;

    void Update()
    {
        if (OxygenTank.Instance == null) return;

        // Always allow refilling even during tutorial
        if (OxygenTank.Instance.isRefilling)
        {
            OxygenTank.Instance.RefillOxygen(refillRate * Time.deltaTime);
            canPlayBreathAudio = true;
            return;
        }

        if (disabled) return;

        if (OxygenTank.Instance.oxygenLevel > 0f)
            _deathTriggered = false;

        if (OxygenTank.Instance.oxygenLevel > 5f)
        {
            _chokingTriggered = false;
            if (chokingAudio != null && chokingAudio.isPlaying)
                chokingAudio.Stop();
        }

        if (breathInput != null && breathInput.isBreathing)
        {
            OxygenTank.Instance.UseOxygen(drainRate * Time.deltaTime);

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
            if (audioSource != null && audioSource.isPlaying)
                audioSource.Stop();
        }

        if (OxygenTank.Instance.oxygenLevel <= 5f && !_chokingTriggered)
        {
            _chokingTriggered = true;
            if (chokingAudio != null)
            {
                chokingAudio.loop = false;
                chokingAudio.Play();
            }
        }

        if (OxygenTank.Instance.oxygenLevel <= 0f && !_deathTriggered)
        {
            _deathTriggered = true;
            if (chokingAudio != null) chokingAudio.Stop();
            OnPlayerDied();
        }
    }

    private void OnPlayerDied()
    {
        if (RespawnManager.Instance != null)
        {
            VRDebugHUD.Instance?.SetStatus("Respawn() called OK");
            RespawnManager.Instance.Respawn();
        }
        else
        {
            VRDebugHUD.Instance?.SetStatus("RespawnManager is NULL!");
        }
    }
}