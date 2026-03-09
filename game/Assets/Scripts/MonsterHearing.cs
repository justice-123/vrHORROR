using UnityEngine;

public class MonsterHearing : MonoBehaviour
{
    [Header("Input")]
    public BreathInputML breath;
    public Transform player;

    [Header("Hearing")]
    public float hearingRadius = 6f;

    [Tooltip("Suspicion gain is scaled by distance - full effect at 0, zero at hearingRadius")]
    public bool useDistanceFalloff = true;

    [Header("Suspicion")]
    [Range(0f, 1f)]
    public float suspicion;

    [Tooltip("Minimum breath intensity (0-1) before the monster notices")]
    public float intensityThreshold = 0.2f;

    [Tooltip("How fast suspicion rises per second at full intensity")]
    public float gainPerSecond = 2.5f;

    [Tooltip("How fast suspicion falls per second when no breath is heard")]
    public float decayPerSecond = 2.0f;

    [Header("Panic Bonus")]
    [Tooltip("BPM above this counts as panicked breathing")]
    public float panicBpmThreshold = 25f;

    [Tooltip("Extra multiplier applied to gain when player is panicking")]
    public float panicGainMultiplier = 1.8f;

    [Header("Debug")]
    public bool isHearing;
    public float debugDistanceFactor;

    void Update()
    {
        if (!breath || !player) return;

        float distance = Vector3.Distance(transform.position, player.position);
        bool inRange = distance <= hearingRadius;

        // Distance falloff: 1.0 at distance 0, 0.0 at hearingRadius
        float distFactor = 1f;
        if (useDistanceFalloff && hearingRadius > 0f)
            distFactor = 1f - Mathf.Clamp01(distance / hearingRadius);
        debugDistanceFactor = distFactor;

        // Use the new spectral-analysis outputs from BreathInput
        bool breathDetected = breath.isBreathing;
        float intensity = breath.breathIntensity01;

        bool heard = inRange && breathDetected && intensity >= intensityThreshold;
        isHearing = heard;

        if (heard)
        {
  
            suspicion += intensity * distFactor * gainPerSecond * Time.deltaTime;
        }
        else
        {
            suspicion -= decayPerSecond * Time.deltaTime;
        }

        suspicion = Mathf.Clamp01(suspicion);
    }
}