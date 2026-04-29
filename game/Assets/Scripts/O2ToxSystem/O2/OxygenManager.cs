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


    private bool[] o2Announced = new bool[4]; // 75, 50, 20, 10

  

    void Update()
    {
        if (disabled) return;

        if (OxygenTank.Instance == null)
        {
            VRDebugHUD.Instance?.SetStatus("OxygenTank NULL - returning");
            return;
        }

        if (OxygenTank.Instance.oxygenLevel > 0f)
            _deathTriggered = false;

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
            VRDebugHUD.Instance?.SetStatus("O2=0 detected, calling Respawn...");
            OnPlayerDied();
        }
        CheckO2Announcements();
    }

    void CheckO2Announcements()
    {
        float o2 = OxygenTank.Instance.oxygenLevel;
        float[] thresholds = { 75f, 50f, 20f, 10f };
        string[] messages = { "Oxygen at 75 percent", "Oxygen at 50 percent", "Warning Oxygen at 20 percent", "Warning Oxygen at 10 percent" };

        for (int i = 0; i < thresholds.Length; i++)
        {
            if (!o2Announced[i] && o2 <= thresholds[i])
            {
                o2Announced[i] = true;
            }
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