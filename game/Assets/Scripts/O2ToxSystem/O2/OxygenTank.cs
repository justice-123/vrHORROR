using UnityEngine;

public class OxygenTank : MonoBehaviour
{
    public static OxygenTank Instance { get; private set; }
    void Awake() { Instance = this; }

    [Range(0f, 100f)]
    public float oxygenLevel = 100f;
    public bool isRefilling = false;

    public void RefillOxygen(float amount) =>
        oxygenLevel = Mathf.Clamp(oxygenLevel + amount, 0f, 100f);

    public void UseOxygen(float amount) =>
        oxygenLevel = Mathf.Clamp(oxygenLevel - amount, 0f, 100f);
}