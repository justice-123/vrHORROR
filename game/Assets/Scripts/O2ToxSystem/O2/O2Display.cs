using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class O2Display : MonoBehaviour
{
    public OxygenTank tank;
    public TextMeshProUGUI oxygenText;
    public Image oxygenBarFill;

    [Header("Colours")]
    public Color normalColor = Color.green;
    public Color warningColor = new Color(1f, 0.5f, 0f); // orange
    public Color dangerColor = Color.red;

    [Header("Thresholds")]
    public float warningThreshold = 50f;
    public float dangerThreshold = 15f;

    [Header("Flashing")]
    public float flashSpeed = 6f;
    public float lowAlpha = 0.2f;
    public float highAlpha = 1f;

    void Update()
    {
        if (tank == null) return;

        float oxygen = tank.oxygenLevel;
        float normalizedOxygen = oxygen / 100f;

        // Update text
        if (oxygenText != null)
            oxygenText.text = Mathf.RoundToInt(oxygen) + "%";

        // Update bar fill amount
        if (oxygenBarFill != null)
            oxygenBarFill.fillAmount = normalizedOxygen;

        // Smooth colour: green → orange between 100% and warningThreshold
        //                orange → red   between warningThreshold and dangerThreshold
        Color targetColor;
        if (oxygen > warningThreshold)
        {
            float t = 1f - ((oxygen - warningThreshold) / (100f - warningThreshold));
            targetColor = Color.Lerp(normalColor, warningColor, t);
        }
        else if (oxygen > dangerThreshold)
        {
            float t = 1f - ((oxygen - dangerThreshold) / (warningThreshold - dangerThreshold));
            targetColor = Color.Lerp(warningColor, dangerColor, t);
        }
        else
        {
            // Below danger threshold — flash red
            float alpha = Mathf.Lerp(lowAlpha, highAlpha, (Mathf.Sin(Time.time * flashSpeed) + 1f) * 0.5f);
            targetColor = dangerColor;
            targetColor.a = alpha;
        }

        if (oxygenText != null) oxygenText.color = targetColor;
        if (oxygenBarFill != null) oxygenBarFill.color = targetColor;
    }
}