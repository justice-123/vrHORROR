using UnityEngine;

public class RoomEntranceTrigger : MonoBehaviour
{
    public ToiletDoor door; // drag the door in here in the inspector

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            door.SlamDoor();
    }
}