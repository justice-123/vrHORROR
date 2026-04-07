using UnityEngine;

public class KeypadLight : MonoBehaviour
{
    public Light keypadLight;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        keypadLight.enabled = false;
    }

    public void enableKeyPadLight()
    {
        keypadLight.enabled = true;
    }
}
