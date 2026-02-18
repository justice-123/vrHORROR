using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class PanicBurstVolume : MonoBehaviour
{
    [Header("References")]
    public BreathSpikeDetector spikeDetector;
    public Volume panicVolume; // Volume B (Spike/Panic)

    [Header("Burst Shape")]
    [Tooltip("How fast the effect ramps up to full.")]
    public float attack = 0.08f;

    [Tooltip("How long it stays near full before fading out.")]
    public float hold = 0.12f;

    [Tooltip("How fast it fades back to 0.")]
    public float release = 0.7f;

    [Tooltip("Max weight during burst.")]
    [Range(0f, 1f)] public float peakWeight = 1f;

    private Coroutine routine;

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
            panicVolume.weight = 0f;
    }

    void HandleSpike()
    {
        if (panicVolume == null) return;

        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(Burst());
    }

    IEnumerator Burst()
    {
        // Attack: 0 -> peak
        float t = 0f;
        float start = panicVolume.weight;

        while (t < attack)
        {
            t += Time.deltaTime;
            float a = (attack <= 0f) ? 1f : Mathf.Clamp01(t / attack);
            panicVolume.weight = Mathf.Lerp(start, peakWeight, a);
            yield return null;
        }
        panicVolume.weight = peakWeight;

        // Hold
        if (hold > 0f)
            yield return new WaitForSeconds(hold);

        // Release: peak -> 0
        t = 0f;
        while (t < release)
        {
            t += Time.deltaTime;
            float a = (release <= 0f) ? 1f : Mathf.Clamp01(t / release);
            panicVolume.weight = Mathf.Lerp(peakWeight, 0f, a);
            yield return null;
        }
        panicVolume.weight = 0f;
        routine = null;
    }
}
