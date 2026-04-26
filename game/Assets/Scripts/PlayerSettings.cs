using UnityEngine;

public class PlayerSettings : MonoBehaviour
{

    public enum TurningMethod {Smooth, Snap}
    public TurningMethod turningmethod;
    public float vignetteIntensityMultiplier = 1f;


    public static PlayerSettings Instance { get; private set; }
    
    private void Awake()
    {
        Instance = this;
        turningmethod = TurningMethod.Smooth;
    }

    public void SmoothTurning()
    {
        turningmethod = TurningMethod.Smooth;
    }

    public void SnapTurning()
    {
        turningmethod = TurningMethod.Snap;
    }

    public void SetVignetteIntensity(float multiplier)
    {
        vignetteIntensityMultiplier = multiplier;
    }

    
}
