using UnityEngine;

public class ToiletLight : MonoBehaviour
{

    public Light spotLight;
    public Light pointLight;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        spotLight.enabled = false;
        pointLight.enabled = false;
    }

    public void enableLight()
    {
        spotLight.enabled = true;
        pointLight.enabled = true;
    }
}