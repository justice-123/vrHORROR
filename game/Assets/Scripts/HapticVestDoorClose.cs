using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HapticVestDoorClose : MonoBehaviour
{
    [SerializeField] AudioSource doorSlam;
    [SerializeField] AudioSource doorOpen;
    [SerializeField] GameObject theDoor;

    void OnTriggerEnter(Collider other)
    {
        theDoor.GetComponent<Animator>().Play("haptic_door_close");
        doorSlam.Play();
        this.GetComponent<BoxCollider>().enabled = false;
        StartCoroutine(WaitThenOpen());
    }

    private IEnumerator WaitThenOpen()
    {
        yield return new WaitForSeconds(12f);
        theDoor.GetComponent<Animator>().Play("haptic_door_open");
        doorOpen.Play();
    }
}