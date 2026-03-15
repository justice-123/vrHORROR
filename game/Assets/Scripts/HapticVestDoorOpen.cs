using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HapticVestDoorOpen : MonoBehaviour
{

    [SerializeField] AudioSource doorSlam;
    [SerializeField] GameObject theDoor;

    void OnTriggerEnter(Collider other)
    {
        theDoor.GetComponent<Animator>().Play("hapticVestOpen");
        doorSlam.Play();
        this.GetComponent<BoxCollider>().enabled = false;
    }
}
