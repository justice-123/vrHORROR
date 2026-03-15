using UnityEngine;
using TMPro;

public class O2Display : MonoBehaviour
{
    public OxygenTank tank;
    public TextMeshProUGUI oxygenText;

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
        if (tank == null || oxygenText == null) return;

        float oxygen = tank.oxygenLevel;
        oxygenText.text = Mathf.RoundToInt(oxygen) + "%";

        if (oxygen <= dangerThreshold)
        {
            float alpha = Mathf.Lerp(
                lowAlpha,
                highAlpha,
                (Mathf.Sin(Time.time * flashSpeed) + 1f) * 0.5f
            );

            Color flashingRed = dangerColor;
            flashingRed.a = alpha;
            oxygenText.color = flashingRed;
        }
        else if (oxygen <= warningThreshold)
        {
            oxygenText.color = warningColor;
        }
        else
        {
            oxygenText.color = normalColor;
        }
    }
}