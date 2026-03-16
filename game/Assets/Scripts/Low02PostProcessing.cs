using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class LowO2PostProcessing : MonoBehaviour
{
    public OxygenTank tank;
    public Volume horrorVolume;

    [Header("Thresholds")]
    public float dangerThreshold = 20f;

    [Header("Vignette")]
    public Color vignetteColor = Color.red;
    public float minVignetteIntensity = 0.35f;
    public float maxVignetteIntensity = 0.8f;
    public float vignetteSmoothness = 0.7f;

    [Header("Pulse")]
    public float flashSpeed = 3f;

    private Vignette vignette;

    void Start()
    {
        if (horrorVolume == null)
        {
            Debug.LogError("LowO2PostProcessing: No Volume assigned.");
            return;
        }

        if (!horrorVolume.profile.TryGet(out vignette))
        {
            Debug.LogError("LowO2PostProcessing: No Vignette override found on the assigned Volume profile.");
            return;
        }

        vignette.color.overrideState = true;
        vignette.intensity.overrideState = true;
        vignette.smoothness.overrideState = true;

        vignette.color.value = vignetteColor;
        vignette.smoothness.value = vignetteSmoothness;
        vignette.intensity.value = 0f;
    }

    void Update()
    {
        if (tank == null || vignette == null)
            return;

        if (tank.oxygenLevel <= dangerThreshold)
        {
            float pulse = (Mathf.Sin(Time.time * flashSpeed) + 1f) * 0.5f;
            vignette.intensity.value = Mathf.Lerp(minVignetteIntensity, maxVignetteIntensity, pulse);
        }
        else
        {
            vignette.intensity.value = 0f;
        }
    }
}