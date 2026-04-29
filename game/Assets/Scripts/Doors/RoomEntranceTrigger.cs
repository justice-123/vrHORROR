using UnityEngine;

public class RoomEntranceTrigger : MonoBehaviour
{
    public ToiletDoor door;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            door.SlamDoor(GetComponent<Collider>()); // passes its own collider to be disabled
    }
}