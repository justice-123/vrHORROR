using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class HighToxicityPostProcessing : MonoBehaviour
{
    public ToxicityDevice toxicityDevice;
    public Volume horrorVolume;

    [Header("Threshold")]
    public float dangerThreshold = 85f;

    [Header("Green Tint")]
    public Color safeColor = Color.white;
    public Color toxicColor = new Color(0.75f, 1f, 0.75f);

    [Header("Effects")]
    public float maxChromaticAberration = 0.08f;
    public float maxLensDistortion = -0.06f;

    private ColorAdjustments colorAdjustments;
    private ChromaticAberration chromaticAberration;
    private LensDistortion lensDistortion;

    void Start()
    {
        if (horrorVolume == null)
        {
            Debug.LogError("HighToxicityPostProcessing: No Volume assigned.");
            return;
        }

        if (!horrorVolume.profile.TryGet(out colorAdjustments))
            Debug.LogError("HighToxicityPostProcessing: No Color Adjustments override found.");

        if (!horrorVolume.profile.TryGet(out chromaticAberration))
            Debug.LogError("HighToxicityPostProcessing: No Chromatic Aberration override found.");

        if (!horrorVolume.profile.TryGet(out lensDistortion))
            Debug.LogError("HighToxicityPostProcessing: No Lens Distortion override found.");

        if (colorAdjustments != null)
        {
            colorAdjustments.colorFilter.overrideState = true;
            colorAdjustments.colorFilter.value = safeColor;
        }

        if (chromaticAberration != null)
        {
            chromaticAberration.intensity.overrideState = true;
            chromaticAberration.intensity.value = 0f;
        }

        if (lensDistortion != null)
        {
            lensDistortion.intensity.overrideState = true;
            lensDistortion.intensity.value = 0f;
        }
    }

    void Update()
    {
        if (toxicityDevice == null) return;

        float toxicity = toxicityDevice.toxicityLevel;

        if (toxicity >= dangerThreshold)
        {
            float severity = Mathf.InverseLerp(dangerThreshold, 100f, toxicity);
            float pulse = toxicityDevice.currentPulse01;

            float effectStrength = Mathf.Lerp(0.4f, 1f, pulse) * severity;

            if (colorAdjustments != null)
                colorAdjustments.colorFilter.value = Color.Lerp(safeColor, toxicColor, effectStrength);

            if (chromaticAberration != null)
                chromaticAberration.intensity.value = maxChromaticAberration * effectStrength;

            if (lensDistortion != null)
                lensDistortion.intensity.value = maxLensDistortion * effectStrength;
        }
        else
        {
            if (colorAdjustments != null)
                colorAdjustments.colorFilter.value = safeColor;

            if (chromaticAberration != null)
                chromaticAberration.intensity.value = 0f;

            if (lensDistortion != null)
                lensDistortion.intensity.value = 0f;
        }
    }
}