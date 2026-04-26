using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;

public class O2BlobBreath : MonoBehaviour
{
    [SerializeField] private ParticleSystem sparkle;
    [Header("References")]
    [SerializeField] private SplineContainer splineContainer;
    [SerializeField] private Transform blob;
    [Header("Movement")]
    [SerializeField] private float travelTime = 0.8f;
    [SerializeField] private float pulseInterval = 0.8f;
    [SerializeField] private bool faceDirection = true;
    [Header("Fade")]
    [SerializeField] private float fadeOutDuration = 0.4f;

    private bool isTravelling = false;
    private bool wasBreathing = false;
    private bool isFadingOut = false;
    private float t = 0f;
    private float pulseTimer = 0f;
    private float fadeTimer = 0f;
    private Renderer blobRenderer;
    private Color originalColor;

    void Start()
    {
        if (blob != null)
        {
            blob.gameObject.SetActive(false);
            blobRenderer = blob.GetComponent<Renderer>();
            if (blobRenderer != null)
                originalColor = blobRenderer.material.color;
        }
        if (sparkle != null)
            sparkle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    void Update()
    {
        if (splineContainer == null || blob == null || OxygenManager.Instance == null)
            return;

        bool isBreathing = OxygenManager.Instance.breathInput != null &&
                           OxygenManager.Instance.breathInput.isBreathing;

        // Started breathing
        if (isBreathing && !wasBreathing)
        {
            isFadingOut = false;
            if (blobRenderer != null)
                blobRenderer.material.color = originalColor;
            pulseTimer = 0f;
            StartPulse();
        }

        if (isBreathing)
        {
            pulseTimer += Time.deltaTime;
            if (!isTravelling && pulseTimer >= pulseInterval)
            {
                StartPulse();
                pulseTimer = 0f;
            }
        }
        else if (!isBreathing && wasBreathing)
        {
            // Just stopped breathing — begin fade
            pulseTimer = 0f;
            isFadingOut = true;
            fadeTimer = 0f;
            if (sparkle != null)
                sparkle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        // Fade out
        if (isFadingOut && blobRenderer != null)
        {
            fadeTimer += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, fadeTimer / fadeOutDuration);
            Color c = originalColor;
            c.a = alpha;
            blobRenderer.material.color = c;

            if (fadeTimer >= fadeOutDuration)
            {
                isFadingOut = false;
                isTravelling = false;
                blob.gameObject.SetActive(false);
                if (blobRenderer != null)
                    blobRenderer.material.color = originalColor;
            }
        }

        if (isTravelling)
            MoveBlobAlongSpline();

        wasBreathing = isBreathing;
    }

    void StartPulse()
    {
        t = 0f; // start at player end, travel toward tank
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

    void MoveBlobAlongSpline()
    {
        t += Time.deltaTime / travelTime; // travel from 1 → 0 (player → tank... wait, tank → player means 0 → 1)
        t = Mathf.Clamp01(t);

        float3 pos = splineContainer.EvaluatePosition(t);
        float3 tangent = splineContainer.EvaluateTangent(t);
        blob.position = (Vector3)pos;

        if (faceDirection && math.lengthsq(tangent) > 0.0001f)
            blob.rotation = Quaternion.LookRotation((Vector3)tangent);

        if (t >= 1f)
        {
            isTravelling = false;
            if (sparkle != null)
                sparkle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            blob.gameObject.SetActive(false);
        }
    }
}