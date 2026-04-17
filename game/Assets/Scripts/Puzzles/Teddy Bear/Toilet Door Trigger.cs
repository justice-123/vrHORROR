using UnityEngine;

public class ToiletDoorTrigger : MonoBehaviour
{
    
    public bool triggered;
    public ToiletDoor toiletDoor;
    public AudioSource toiletAudio;

    void Start()
    {
        triggered = false;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!triggered)
        {
            triggered = true;
            toiletDoor.visitNursery();
            toiletAudio.Play();
        }
    }


}
