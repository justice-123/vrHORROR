using UnityEngine;

public class UVLamp : MonoBehaviour
{

    public Light uvLight;
    public UVText uvText;
    public AudioSource audioSource;

    private bool isOn = false;
    

    public void ToggleLight()
    {
        isOn = !isOn;

        if (isOn)
        {
            uvLight.enabled = true;
            uvText.turnTextOn();
            audioSource.Play();
        } else
        {
            uvLight.enabled = false;
            uvText.turnTextOff();
        }
    }
}
