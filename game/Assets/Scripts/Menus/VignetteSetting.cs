using UnityEngine;

public class VignetteSetting : MonoBehaviour
{
    
    void Start()
    {
        PlayerSettings.Instance.SetVignetteIntensity(1f);
    }

    public void UpdateIntensity(float multiplier)
    {
        PlayerSettings.Instance.SetVignetteIntensity(multiplier);
    }


}
