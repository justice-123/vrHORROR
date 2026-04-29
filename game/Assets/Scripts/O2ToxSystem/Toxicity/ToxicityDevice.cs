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

    private bool[] toxAnnounced = new bool[4]; // 75, 50, 20, 10 (going up)

    void CheckToxAnnouncements()
    {
        float[] thresholds = { 10f, 20f, 50f, 75f };
        string[] messages = { "Toxicity at 10 percent", "Toxicity at 20 percent", "Toxicity at 50 percent", "Warning Toxicity at 75 percent" };

        for (int i = 0; i < thresholds.Length; i++)
        {
            if (!toxAnnounced[i] && toxicityLevel >= thresholds[i])
            {
                toxAnnounced[i] = true;
            }
        }
    }

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

        CheckToxAnnouncements();
    }

}