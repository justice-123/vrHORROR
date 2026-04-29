using UnityEngine;

public class ToiletDoorTrigger : MonoBehaviour
{
    
    public bool triggered;
    public ToiletLight toiletLight;
    public ToiletDoor toiletDoor;

    void Start()
    {
        triggered = false;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!triggered)
        {
            triggered = true;
            toiletDoor.inNursery = true;
            toiletLight.enableLight();
        }
    }


}
