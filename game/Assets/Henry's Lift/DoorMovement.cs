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
        StartCoroutine(moveDoor(leftDoor, -1f, 5f));
        StartCoroutine(moveDoor(rightDoor, 1f, 5f));
        doorClose.Play();

        StartCoroutine(LiftLoadingAreas());

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
        yield return new WaitForSeconds(10f);

        MySceneManager.Instance.UnloadOldScene("First Area");
        MySceneManager.Instance.LoadNewScene("Second Area");
    }

}