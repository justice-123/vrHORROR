using Oculus.Interaction.Body.Samples;
using UnityEngine;

public class DoorLock : MonoBehaviour
{

    public FrontDoor frontDoor;
    public bool unlocked;
    public AudioSource lockAudio;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        unlocked = false;
    }

    // Update is called once per frame
    public void OnTriggerEnter(Collider other)
    {
        if (!unlocked && InventoryManager.Instance.getActiveItemID() == "doorkey")
        {
            unlocked = true;
            frontDoor.unlockLock();
            lockAudio.Play();
            InventoryManager.Instance.DeleteCurrentItem();
        }
    }
}
