using UnityEngine;

public class EChairTrigger : MonoBehaviour
{
    public DoorFallingOver door;
    bool triggered;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        triggered = false;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!triggered && other.tag == "Player")
        {
            triggered = true;
            door.visitEChair();
        }
    }
}
