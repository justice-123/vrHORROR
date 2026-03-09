using UnityEngine;

public class OxygenTank : MonoBehaviour
{
    [Range(0f, 100f)]
    public float oxygenLevel = 100f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void UseOxygen(float amount)
    {
        oxygenLevel = Mathf.Clamp(oxygenLevel - amount, 0f, 100f);
    }

    public void RefillOxygen(float amount)
    {
        oxygenLevel = Mathf.Clamp(oxygenLevel + amount, 0f, 100f);
    }

    public float GetOxygenNormalised()
    {
        return oxygenLevel / 100f;
    }
}
