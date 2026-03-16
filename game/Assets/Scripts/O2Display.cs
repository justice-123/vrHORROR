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
    public float warningThreshold = 40f;
    public float dangerThreshold = 10f;

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
        {
            oxygenText.text = Mathf.RoundToInt(oxygen) + "%";
        }

        // Update bar amount
        if (oxygenBarFill != null)
        {
            oxygenBarFill.fillAmount = normalizedOxygen;
        }

        // Update colours
        if (oxygen <= dangerThreshold)
        {
            float alpha = Mathf.Lerp(
                lowAlpha,
                highAlpha,
                (Mathf.Sin(Time.time * flashSpeed) + 1f) * 0.5f
            );

            Color flashingRed = dangerColor;
            flashingRed.a = alpha;

            if (oxygenText != null)
                oxygenText.color = flashingRed;

            if (oxygenBarFill != null)
                oxygenBarFill.color = flashingRed;
        }
        else if (oxygen <= warningThreshold)
        {
            if (oxygenText != null)
                oxygenText.color = warningColor;

            if (oxygenBarFill != null)
                oxygenBarFill.color = warningColor;
        }
        else
        {
            if (oxygenText != null)
                oxygenText.color = normalColor;

            if (oxygenBarFill != null)
                oxygenBarFill.color = normalColor;
        }
    }
}