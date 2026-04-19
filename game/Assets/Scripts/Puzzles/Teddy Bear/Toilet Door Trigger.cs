using UnityEngine;

public class ToiletDoorTrigger : MonoBehaviour
{
    
    public bool triggered;
    public ToiletLight toiletLight;

    void Start()
    {
        triggered = false;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!triggered)
        {
            triggered = true;
            toiletLight.enableLight();
        }
    }


}
