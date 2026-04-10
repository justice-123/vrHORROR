using System.Drawing;
using UnityEngine;

public class FlickeringLight : MonoBehaviour
{
    
    public float minIntensity = 0.5f;
    public float maxIntensity = 2f;
    public float speed = 0.1f;

    public Light spotLight;
    public Light pointLight;

    private float targetIntensity;
    private float lastTime;

    private bool turnedOn = true;

    void Start()
    {
        turnedOn = true;
    }

    void Update()
    {
        if (turnedOn) {

            if (Time.time - lastTime > speed)
            {
                targetIntensity = Random.Range(minIntensity, maxIntensity);
                lastTime = Time.time;
            }

            spotLight.intensity = Mathf.Lerp(spotLight.intensity, targetIntensity, Time.deltaTime * 20f);
            pointLight.intensity = spotLight.intensity;
            
        }
    }

    public void TurnOffLight()
    {
        turnedOn = false;
        spotLight.intensity = 0f;
        pointLight.intensity = 0f;
    }



}
