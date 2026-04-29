using UnityEngine;
using UnityEngine.UI;
using static Oculus.Interaction.Context;

public class ToxicityDevice : MonoBehaviour
{
    [Header("Toxicity Settings")]
    [Range(0f, 100f)]
    public float toxicityLevel = 0f;
    public float fillRate = 1f;
    public float drainRate = 5f;
    public bool disabled = false;

    [Header("References")]
    public BreathInputML breathInput;

    [Header("Visuals")]
    public Image barFill;

    public static ToxicityDevice Instance { get; private set; }

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        if (disabled) return;

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
        barFill.fillAmount = toxicityLevel / 100f;
    }
}