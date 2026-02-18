using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Panic meter driven by breath spikes:
/// - A spike (sharp inhale/exhale onset) instantly increases panic01
/// - A "panic window" timer starts, keeping the effect around for a set duration
/// - After the window ends, panic decays normally (or faster if the player is calm)
///
/// Drives panicVolume.weight directly (no per-override scripting needed).
/// </summary>
public class PanicMeterVolume : MonoBehaviour
{
    [Header("References")]
    public BreathSpikeDetector spikeDetector;   // listens for OnSpike + exposes rms/delta for extra scaling
    public Volume panicVolume;                  // your scary Volume (Weight will be driven)
    public BreathInput breathInput;             // optional: used for calm detection via breathNoise01 (0..1)

    [Header("Panic State (read-only)")]
    [Range(0f, 1f)] public float panic01;

    [Header("Spike -> Panic")]
    [Tooltip("How much panic increases per spike (big jump so spikes feel impactful).")]
    [Range(0f, 1f)] public float spikeAdd = 0.5f;

    [Tooltip("Optional: scales spikeDetector.delta into extra panic. Set 0 to disable.")]
    public float extraFromDelta = 1200f;

    [Header("Panic Window (Option A)")]
    [Tooltip("After a spike, panic stays 'latched' for this long.")]
    public float panicWindowSeconds = 1.5f;

    [Tooltip("How fast panic decays during the window (0 = no decay / full hold).")]
    public float windowDecayPerSecond = 0.05f;

    [Header("Decay (after window)")]
    [Tooltip("Normal decay when not calming.")]
    public float baseDecayPerSecond = 0.20f;

    [Tooltip("Faster decay when the player is calm.")]
    public float calmDecayPerSecond = 0.90f;

    [Header("Calm Detection")]
    [Tooltip("If breathNoise01 is below this for a short time, we treat player as calming.")]
    [Range(0f, 1f)] public float calmBreathThreshold01 = 0.18f;

    [Tooltip("Seconds of staying calm before calm decay kicks in.")]
    public float calmGraceSeconds = 0.25f;

    [Header("Debug (read-only)")]
    [SerializeField] private float panicWindowTimer;
    [SerializeField] private float calmTimer;
    [SerializeField] private bool isCalming;

    void OnEnable()
    {
        if (spikeDetector != null)
            spikeDetector.OnSpike += HandleSpike;
    }

    void OnDisable()
    {
        if (spikeDetector != null)
            spikeDetector.OnSpike -= HandleSpike;
    }

    void Start()
    {
        if (panicVolume != null)
            panicVolume.weight = panic01;
    }

    void Update()
    {
        if (panicVolume == null) return;

        // ---- Calm detection (optional) ----
        bool calmNow = false;

        if (breathInput != null)
        {
            calmNow = breathInput.breathNoise01 < calmBreathThreshold01;
        }
        else if (spikeDetector != null)
        {
            // Fallback if you didn't wire BreathInput:
            // treat near-silence as calm
            calmNow = spikeDetector.rms < (spikeDetector.minRms * 1.1f);
        }

        if (calmNow) calmTimer += Time.deltaTime;
        else calmTimer = 0f;

        isCalming = calmTimer >= calmGraceSeconds;

        // ---- Panic window logic ----
        if (panicWindowTimer > 0f)
        {
            panicWindowTimer -= Time.deltaTime;

            // During window: hold / slow decay
            float decay = Mathf.Max(0f, windowDecayPerSecond);
            panic01 = Mathf.MoveTowards(panic01, 0f, decay * Time.deltaTime);
        }
        else
        {
            // After window: normal decay (or faster if calming)
            float decay = isCalming ? calmDecayPerSecond : baseDecayPerSecond;
            panic01 = Mathf.MoveTowards(panic01, 0f, Mathf.Max(0f, decay) * Time.deltaTime);
        }

        // Apply to the volume
        panicVolume.weight = panic01;
    }

    void HandleSpike()
    {
        // Add a chunk of panic immediately
        float add = spikeAdd;

        // Optional scaling by spike strength
        if (spikeDetector != null && extraFromDelta > 0f)
        {
            // delta is raw RMS change; scale and clamp to a reasonable extra bump
            float extra01 = Mathf.Clamp01(spikeDetector.delta * extraFromDelta);
            add += extra01 * 0.25f;
        }

        panic01 = Mathf.Clamp01(panic01 + add);

        // Start or refresh the panic window (keeps effect around even if spike was brief)
        panicWindowTimer = Mathf.Max(panicWindowTimer, panicWindowSeconds);

        // Reset calm timer so a spike can't be instantly "calm"
        calmTimer = 0f;
    }
}
