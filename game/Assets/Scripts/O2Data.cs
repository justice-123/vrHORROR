using UnityEngine;

public class OxygenTank : MonoBehaviour
{
    [Range(0f, 100f)]
    public float oxygenLevel = 100f;

    public BreathInputML breathInput;
    public float drainRate = 5f; // percent per second

    void Update()
    {
        if (breathInput != null && breathInput.isBreathing)
        {
            oxygenLevel = Mathf.Clamp(
                oxygenLevel - drainRate * Time.deltaTime,
                0f,
                100f
            );
        }
    }

    public void UseOxygen(float amount)
    {
        oxygenLevel = Mathf.Clamp(oxygenLevel - amount, 0f, 100f);
    }

    public void RefillOxygen(float amount)
    {
        oxygenLevel = Mathf.Clamp(oxygenLevel + amount, 0f, 100f);
    }
}