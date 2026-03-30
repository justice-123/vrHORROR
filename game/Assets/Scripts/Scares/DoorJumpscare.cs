using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoorJumpscare : MonoBehaviour
{

    [SerializeField] AudioSource pianoScare;
    [SerializeField] AudioSource doorSlam;
    [SerializeField] GameObject theDoor;

    void OnTriggerEnter(Collider other)
    {
        theDoor.GetComponent<Animator>().Play("DoubleDoorSwing");
        pianoScare.Play();
        doorSlam.Play();
        this.GetComponent<BoxCollider>().enabled = false;
    }
}
