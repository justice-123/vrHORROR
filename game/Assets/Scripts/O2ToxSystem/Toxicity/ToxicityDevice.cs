using UnityEngine;

public class ToxicityDevice : MonoBehaviour
{
    [Header("Toxicity")]
    [Range(0f, 100f)]
    public float toxicityLevel = 0f;

    public float fillRate = 1f;   // increases when NOT breathing
    public float drainRate = 5f;  // decreases when breathing

    [Header("References")]
    public BreathInputML breathInput;

    [Header("Visuals")]
    public Renderer lightRenderer;

    public Color startColor = Color.white;
    public Color endColor = Color.green;

    [Header("Emission")]
    public float emissionStrength = 1.5f;

    void Update()
    {
        if (breathInput != null && breathInput.isBreathing)
        {
            toxicityLevel -= drainRate * Time.deltaTime;
        }
        else
        {
            toxicityLevel += fillRate * Time.deltaTime;
        }

        toxicityLevel = Mathf.Clamp(toxicityLevel, 0f, 100f);

        UpdateVisuals();
    }

    void UpdateVisuals()
    {
        if (lightRenderer == null) return;

        float t = toxicityLevel / 100f;
        Color currentColor = Color.Lerp(startColor, endColor, t);

        Material mat = lightRenderer.material;

        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", currentColor);
        else
            mat.color = currentColor;

        if (mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", currentColor * emissionStrength);
        }
    }
}