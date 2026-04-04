using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class SuspicionPostFX : MonoBehaviour
{
    [Header("Inputs")]
    public MonsterHearing hearing;
    public Volume volume;

    [Header("Tuning")]
    public float responseSpeed = 8f;
    [Range(0f, 1f)] public float maxVignette = 0.45f;
    [Range(-100f, 0f)] public float minSaturation = -35f;
    [Range(0f, 100f)] public float maxContrast = 20f;
    public Color dangerTint = new Color(0.9f, 0.2f, 0.2f, 1f);

    Vignette vignette;
    ColorAdjustments colorAdj;

    float sSmooth;

    void Awake()
    {
        if (!volume) volume = GetComponent<Volume>();

        if (!volume || !volume.profile)
        {
            Debug.LogError("SuspicionPostFX: Volume or Volume Profile missing.");
            enabled = false;
            return;
        }

        volume.profile.TryGet(out vignette);
        volume.profile.TryGet(out colorAdj);

        if (vignette == null) Debug.LogWarning("No Vignette override found in Volume Profile.");
        if (colorAdj == null) Debug.LogWarning("No Color Adjustments override found in Volume Profile.");
    }

    void Update()
    {
        if (!hearing) return;

        float s = Mathf.Clamp01(hearing.suspicion);
        float t = 1f - Mathf.Exp(-responseSpeed * Time.deltaTime);
        sSmooth = Mathf.Lerp(sSmooth, s, t);

        if (vignette != null)
        {
            vignette.intensity.value = Mathf.Lerp(0f, maxVignette, sSmooth);
            vignette.color.value = Color.Lerp(Color.black, dangerTint, sSmooth);
        }

        if (colorAdj != null)
        {
            colorAdj.colorFilter.value = Color.Lerp(Color.white, dangerTint, sSmooth * 0.7f);
            colorAdj.saturation.value = Mathf.Lerp(0f, minSaturation, sSmooth);
            colorAdj.contrast.value = Mathf.Lerp(0f, maxContrast, sSmooth);
        }
    }
}
