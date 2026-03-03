using UnityEngine;

public class MonsterHearing : MonoBehaviour
{
    [Header("Input")]
    public BreathInput breath;     
    public Transform player;       

    [Header("Hearing")]
    public float hearingRadius = 6f;

    [Header("Suspicion")]
    [Range(0f, 1f)]
    public float suspicion;

    public float hearThreshold = 0.25f; // how loud before it counts as a breath
    public float gainPerSecond = 2.5f;  // how quickly the suspicioin rises
    public float decayPerSecond = 2.0f; // how quickly the suspicion falls

    void Update()
    {
        if (!breath || !player) return;

        float distance = Vector3.Distance(transform.position, player.position);
        bool inRange = distance <= hearingRadius;

        float loudness = breath.breathNoise01;
        bool heard = inRange && loudness >= hearThreshold;

        if (heard)
        {
            suspicion += loudness * gainPerSecond * Time.deltaTime;
        }
        else
        {
            suspicion -= decayPerSecond * Time.deltaTime;
        }

        suspicion = Mathf.Clamp01(suspicion);
    }
}
