using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class HallucinationPostFX : MonoBehaviour
{
    public ToxicityDevice toxicity;
    public Volume volume;
    public float activateAbove = 40f;

    [Header("Choking Audio")]
    public AudioSource chokingSource;
    public AudioClip chokingClip;

    ChromaticAberration chromatic;
    LensDistortion lensDistortion;
    FilmGrain filmGrain;
    ColorAdjustments colorAdj;

    bool wasActive;

    void Start()
    {
        volume.profile.TryGet(out chromatic);
        volume.profile.TryGet(out lensDistortion);
        volume.profile.TryGet(out filmGrain);
        volume.profile.TryGet(out colorAdj);
    }

    void Update()
    {
        bool isActive = toxicity.toxicityLevel >= activateAbove;

        if (isActive && !wasActive)
        {
            if (chokingSource != null && chokingClip != null)
            {
                chokingSource.clip = chokingClip;
                chokingSource.Play();
            }
        }

        wasActive = isActive;

        if (!isActive)
        {
            SetEffects(0f);
            return;
        }

        float t = (toxicity.toxicityLevel - activateAbove)
                / (100f - activateAbove);
        SetEffects(t);
    }

    void SetEffects(float t)
    {
        if (chromatic != null)
        {
            chromatic.active = t > 0f;
            chromatic.intensity.value = Mathf.Lerp(0f, 0.4f, t);
        }

        if (lensDistortion != null)
        {
            lensDistortion.active = t > 0f;
            lensDistortion.intensity.value = Mathf.Lerp(0f, -0.3f, t);
        }

        if (filmGrain != null)
        {
            filmGrain.active = t > 0f;
            filmGrain.intensity.value = Mathf.Lerp(0f, 0.6f, t);
        }

        if (colorAdj != null)
        {
            colorAdj.active = t > 0f;
            colorAdj.saturation.value = Mathf.Lerp(0f, -100f, t);
            colorAdj.contrast.value = Mathf.Lerp(0f, 40f, t);
        }
    }
}
