using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoorOpen : MonoBehaviour
{


    [SerializeField] AudioSource doorSlam;
    [SerializeField] GameObject theDoor;

    void OnTriggerEnter(Collider other)
    {
        theDoor.GetComponent<Animator>().Play("OpenDoorIntro");
        doorSlam.Play();
        this.GetComponent<BoxCollider>().enabled = false;
    }
}
