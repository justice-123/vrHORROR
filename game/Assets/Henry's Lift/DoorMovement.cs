using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class DoorMovement : MonoBehaviour
{

    public Transform leftDoor;
    public Transform rightDoor;
    public AudioSource doorOpen;
    public AudioSource doorClose;

// move to y = 0
    public void openDoors()
    {
        StartCoroutine(moveDoor(leftDoor, 0f, 5f));
        StartCoroutine(moveDoor(rightDoor, 0f, 5f));
        doorOpen.Play();
    }

    // move left door to y = -1
    // move right door to y = 1
    public void closeDoors()
    {
        if (LiftScare.Instance != null)
        {
            StartCoroutine(LiftScare.Instance.PlayScare());
            return;
        }


        Debug.LogWarning("[Lift] closeDoors called");
        StartCoroutine(moveDoor(leftDoor, -1f, 5f));
        StartCoroutine(moveDoor(rightDoor, 1f, 5f));
        doorClose.Play();

        StartCoroutine(LiftLoadingAreas());




    }

    public void crackDoorsOpen()
    {
        StartCoroutine(moveDoor(leftDoor, -0.3f, 1.5f));
        StartCoroutine(moveDoor(rightDoor, 0.3f, 1.5f));
        doorOpen.Play();
    }


    public IEnumerator moveDoor(Transform door, float targetY, float duration)
    {

        float elapsed = 0f;
        Vector3 startPos = door.localPosition;
        Vector3 endPos = new Vector3(startPos.x, targetY, startPos.z);

        while (elapsed < duration)
        {
            float t = elapsed / duration;

            door.localPosition = Vector3.Lerp(startPos, endPos, t);

            elapsed += Time.deltaTime;

            yield return null;
        }

        door.localPosition = endPos;
    }

    public IEnumerator LiftLoadingAreas()
    {
        Debug.LogWarning("[Lift] LiftLoadingAreas: waiting 5s for doors");
        yield return new WaitForSeconds(5f);
        Debug.LogWarning("[Lift] LiftLoadingAreas: starting area transition");
        yield return StartCoroutine(AreaTransition.Instance.StartAreaTransition());
        Debug.LogWarning("[Lift] LiftLoadingAreas: transition done, opening doors");
        openDoors();
    }

    public IEnumerator TransitionAndOpenDoors()
    {
        yield return StartCoroutine(AreaTransition.Instance.StartAreaTransition());
        openDoors();
    }


    public IEnumerator openDoorsRoutine()
    {
        Coroutine left = StartCoroutine(moveDoor(leftDoor, 0f, 2.5f));
        Coroutine right = StartCoroutine(moveDoor(rightDoor, 0f, 2.5f));
        doorOpen.Play();
        yield return left;
        yield return right;
    }

    public IEnumerator closeDoorsRoutine()
    {
        Coroutine left = StartCoroutine(moveDoor(leftDoor, -1f, 2.5f));
        Coroutine right = StartCoroutine(moveDoor(rightDoor, 1f, 2.5f));
        doorClose.Play();
        yield return left;
        yield return right;
    }

}