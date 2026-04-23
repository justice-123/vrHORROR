using Oculus.Interaction.Samples;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class Wheelchair_Vignette : MonoBehaviour
{

    public Volume volume;
    private Vignette vignette;

    public Transform vrRig;

    public float currentSpeed;
    public float currentRotationSpeed;

    public float maxIntensity = 0.75f;
    public float speedMultiplier = 0.4f;
    public float turnMultiplier = 0.25f;
    public float smoothing = 5f;

    public float currentIntensity;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (volume.profile.TryGet(out vignette))
        {
            vignette.intensity.overrideState = true;
        }
    }

    // Update is called once per frame
    void Update()
    {
        float move = Mathf.Abs(currentSpeed);
        float turn = Mathf.Abs(currentRotationSpeed);

        float combined = (move * speedMultiplier) + (turn * turnMultiplier);

        if (combined < 0.01f) combined = 0f;

        float targetIntensity = Mathf.Clamp(combined, 0f, maxIntensity);

        currentIntensity = Mathf.Lerp(currentIntensity, targetIntensity, Time.deltaTime * smoothing);

        vignette.intensity.value = PlayerSettings.Instance.vignetteIntensityMultiplier * currentIntensity;
    }
}
