using UnityEngine;
using UnityEngine.UI;

public class ToxicityDevice : MonoBehaviour
{
    [Header("Toxicity Settings")]
    [Range(0f, 100f)]
    public float toxicityLevel = 0f;
    public float fillRate = 1f;    // units/sec when NOT breathing
    public float drainRate = 5f;   // units/sec when breathing

    [Header("References")]
    public BreathInputML breathInput;

    [Header("Visuals")]
    public Image barFill;

    void Update()
    {
        bool currentlyBreathing = breathInput != null && breathInput.isBreathing;

        if (currentlyBreathing)
            toxicityLevel -= drainRate * Time.deltaTime;
        else
            toxicityLevel += fillRate * Time.deltaTime;

        toxicityLevel = Mathf.Clamp(toxicityLevel, 0f, 100f);

        UpdateVisuals();
    }

    void UpdateVisuals()
    {
        if (barFill == null) return;

        // fillAmount drives the Filled image — 1 = full bar (100% toxic), 0 = empty
        barFill.fillAmount = toxicityLevel / 100f;
    }
}