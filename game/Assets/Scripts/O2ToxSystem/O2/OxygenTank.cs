using UnityEngine;

public class OxygenTank : MonoBehaviour
{
    public static OxygenTank Instance { get; private set; }

    // Fires when oxygen crosses a 25% threshold going down (75, 50, 25, 0)
    public static event System.Action<int> OnOxygenThresholdCrossed;

    [Range(0f, 100f)]
    public float oxygenLevel = 100f;
    public bool isRefilling = false;

    private int nextThreshold = 75;

    void Awake()
    {
        Instance = this;
    }

    public void RefillOxygen(float amount)
    {
        oxygenLevel = Mathf.Clamp(oxygenLevel + amount, 0f, 100f);
        ResetThresholds();
    }

    public void UseOxygen(float amount)
    {
        oxygenLevel = Mathf.Clamp(oxygenLevel - amount, 0f, 100f);
        CheckThreshold();
    }

    private void CheckThreshold()
    {
        while (nextThreshold >= 25 && oxygenLevel <= nextThreshold)
        {
            OnOxygenThresholdCrossed?.Invoke(nextThreshold);
            nextThreshold -= 25;
        }
    }

    private void ResetThresholds()
    {
        nextThreshold = 75;
        while (nextThreshold >= 25 && oxygenLevel <= nextThreshold)
            nextThreshold -= 25;
    }
}