using UnityEngine;
using UnityEngine.UI;

public class O2HUDEffect : MonoBehaviour
{
    public OxygenTank tank;
    public Image redVignette;

    [Header("Thresholds")]
    public float dangerThreshold = 10f;

    [Header("Flashing")]
    public float flashSpeed = 4f;
    public float minAlpha = 0.05f;
    public float maxAlpha = 0.25f;

    void Update()
    {
        if (tank == null || redVignette == null) return;

        Color c = redVignette.color;

        if (tank.oxygenLevel <= dangerThreshold)
        {
            float flash = (Mathf.Sin(Time.time * flashSpeed) + 1f) * 0.5f;
            c.a = Mathf.Lerp(minAlpha, maxAlpha, flash);
        }
        else
        {
            c.a = 0f;
        }

        redVignette.color = c;
    }
}