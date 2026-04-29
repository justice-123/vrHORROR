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

    [Header("Film Grain")]
    public float maxGrainIntensity = 1f;

    [Header("Fade In")]
    public float fadeInDuration = 1.5f;

    private Vignette _vignette;
    private FilmGrain _filmGrain;
    private float _fadeT = 0f;

    void Start()
    {
        if (horrorVolume == null) { Debug.LogError("LowO2PostProcessing: No Volume assigned."); return; }

        horrorVolume.profile.TryGet(out _vignette);
        horrorVolume.profile.TryGet(out _filmGrain);

        if (_vignette != null)
        {
            _vignette.color.overrideState = true;
            _vignette.intensity.overrideState = true;
            _vignette.smoothness.overrideState = true;
            _vignette.color.value = vignetteColor;
            _vignette.smoothness.value = vignetteSmoothness;
            _vignette.intensity.value = 0f;
        }

        if (_filmGrain != null)
        {
            _filmGrain.intensity.overrideState = true;
            _filmGrain.response.overrideState = true;
            _filmGrain.intensity.value = 0f;
            _filmGrain.response.value = 0.8f;
        }
    }

    void Update()
    {
        if (tank == null) return;

        if (tank.oxygenLevel <= dangerThreshold)
        {
            _fadeT = Mathf.MoveTowards(_fadeT, 1f, Time.deltaTime / fadeInDuration);

            float pulse = (Mathf.Sin(Time.time * flashSpeed) + 1f) * 0.5f;
            float vignetteIntensity = Mathf.Lerp(minVignetteIntensity, maxVignetteIntensity, pulse);

            if (_vignette != null) _vignette.intensity.value = Mathf.Lerp(0f, vignetteIntensity, _fadeT);
            if (_filmGrain != null) _filmGrain.intensity.value = Mathf.Lerp(0f, maxGrainIntensity, _fadeT);
        }
        else
        {
            _fadeT = 0f;
            if (_vignette != null) _vignette.intensity.value = 0f;
            if (_filmGrain != null) _filmGrain.intensity.value = 0f;
        }
    }
}