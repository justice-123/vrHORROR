using UnityEngine;

public class LightUpSign : MonoBehaviour
{

    public Light lamp;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        lamp.intensity = 0;
    }

    public void turnLightOn()
    {
        lamp.intensity = 0.33f;
    }

    public void turnLightOff()
    {
        lamp.intensity = 0f;
    }

    private void OnTriggerEnter(Collider coll)
    {
        if (coll.CompareTag("Player"))
        {
            turnLightOn();
        }
    }
}
