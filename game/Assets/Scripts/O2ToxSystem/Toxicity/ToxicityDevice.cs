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
    public RectTransform barFill;
    public float barMaxHeight = 100f; // set this to the full height of the bar in pixels

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
        if (barFill == null) return;
        float t = toxicityLevel / 100f;
        barFill.sizeDelta = new Vector2(barFill.sizeDelta.x, barMaxHeight * t);
    }
}