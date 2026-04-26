using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;

public class O2BlobRefill : MonoBehaviour
{
    [SerializeField] private ParticleSystem sparkle;

    [Header("References")]
    [SerializeField] private SplineContainer splineContainer;
    [SerializeField] private Transform blob;

    [Header("Movement")]
    [SerializeField] private float travelTime = 0.8f;
    [SerializeField] private float pulseInterval = 0.8f;
    [SerializeField] private bool faceDirection = true;

    private bool isTravelling = false;
    private bool wasRefilling = false;
    private float t = 0f;
    private float pulseTimer = 0f;

    void Start()
    {
        if (blob != null)
            blob.gameObject.SetActive(false);

        if (sparkle != null)
            sparkle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    void Update()
    {
        if (splineContainer == null || blob == null || OxygenTank.Instance == null)
            return;

        bool isRefilling = OxygenTank.Instance.isRefilling;

        if (isRefilling && !wasRefilling)
        {
            pulseTimer = 0f;
            StartPulse();
        }

        if (isRefilling)
        {
            pulseTimer += Time.deltaTime;

            if (!isTravelling && pulseTimer >= pulseInterval)
            {
                StartPulse();
                pulseTimer = 0f;
            }
        }
        else
        {
            pulseTimer = 0f;

            if (isTravelling)
                StopPulse();
        }

        if (isTravelling)
            MoveBlobAlongSpline();

        wasRefilling = isRefilling;
    }

    void StartPulse()
    {
        t = 0f;
        isTravelling = true;
        blob.gameObject.SetActive(true);

        float3 startPos = splineContainer.EvaluatePosition(0f);
        blob.position = (Vector3)startPos;

        if (sparkle != null)
        {
            sparkle.Clear();
            sparkle.Play();
        }
    }

    void StopPulse()
    {
        isTravelling = false;

        if (sparkle != null)
            sparkle.Stop(true, ParticleSystemStopBehavior.StopEmitting);

        blob.gameObject.SetActive(false);
    }

    void MoveBlobAlongSpline()
    {
        t += Time.deltaTime / travelTime;
        t = Mathf.Clamp01(t);

        float3 pos = splineContainer.EvaluatePosition(t);
        float3 tangent = splineContainer.EvaluateTangent(t);

        blob.position = (Vector3)pos;

        if (faceDirection && math.lengthsq(tangent) > 0.0001f)
        {
            blob.rotation = Quaternion.LookRotation((Vector3)tangent);
        }

        if (t >= 1f)
        {
            isTravelling = false;

            if (sparkle != null)
                sparkle.Stop(true, ParticleSystemStopBehavior.StopEmitting);

            blob.gameObject.SetActive(false);
        }
    }
}