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
    public AudioSource chokingAudio;  // drag your choking sound in here

    private bool canPlayBreathAudio = false;
    private bool _deathTriggered = false;
    private bool _chokingTriggered = false;

    void Update()
    {
        if (OxygenTank.Instance == null)
        {
            VRDebugHUD.Instance?.SetStatus("OxygenTank NULL - returning");
            return;
        }

        if (OxygenTank.Instance.oxygenLevel > 0f)
            _deathTriggered = false;

        // Reset choking flag once oxygen recovers above 5%
        if (OxygenTank.Instance.oxygenLevel > 5f)
        {
            _chokingTriggered = false;
            if (chokingAudio != null && chokingAudio.isPlaying)
                chokingAudio.Stop();
        }

        if (OxygenTank.Instance.isRefilling)
        {
            OxygenTank.Instance.oxygenLevel = Mathf.Clamp(
                OxygenTank.Instance.oxygenLevel + refillRate * Time.deltaTime, 0f, 100f);
            canPlayBreathAudio = true;
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
            if (audioSource != null && audioSource.isPlaying)
                audioSource.Stop();
        }

        // Choking at 5%
        if (OxygenTank.Instance.oxygenLevel <= 5f && !_chokingTriggered)
        {
            _chokingTriggered = true;
            if (chokingAudio != null)
            {
                chokingAudio.loop = false;
                chokingAudio.Play();
            }
        }

        // Death at 0%
        if (OxygenTank.Instance.oxygenLevel <= 0f && !_deathTriggered)
        {
            _deathTriggered = true;
            if (chokingAudio != null) chokingAudio.Stop();
            VRDebugHUD.Instance?.SetStatus("O2=0 detected, calling Respawn...");
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