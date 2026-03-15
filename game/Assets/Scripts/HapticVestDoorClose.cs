using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HapticVestDoorClose : MonoBehaviour
{

    [SerializeField] AudioSource doorSlam;
    [SerializeField] GameObject theDoor;

    void OnTriggerEnter(Collider other)
    {
        theDoor.GetComponent<Animator>().Play("hapticDoorClose");
        doorSlam.Play();
        this.GetComponent<BoxCollider>().enabled = false;
    }
}
