using UnityEngine;

public class OxygenTank : MonoBehaviour
{
    [Range(0f, 100f)]
    public float oxygenLevel = 100f;

    public BreathInputML breathInput;
    public float drainRate = 5f;   // when breathing
    public float refillRate = 25f; // while touching refill cube

    [HideInInspector]
    public bool isRefilling = false;

    void Update()
    {
        if (isRefilling)
        {
            oxygenLevel = Mathf.Clamp(oxygenLevel + refillRate * Time.deltaTime, 0f, 100f);
            return; // ignore breathing while refilling
        }

        if (breathInput != null && breathInput.isBreathing)
        {
            oxygenLevel = Mathf.Clamp(oxygenLevel - drainRate * Time.deltaTime, 0f, 100f);
        }
    }

    public void RefillOxygen(float amount)
    {
        oxygenLevel = Mathf.Clamp(oxygenLevel + amount, 0f, 100f);
    }

    public void UseOxygen(float amount)
    {
        oxygenLevel = Mathf.Clamp(oxygenLevel - amount, 0f, 100f);
    }
}
