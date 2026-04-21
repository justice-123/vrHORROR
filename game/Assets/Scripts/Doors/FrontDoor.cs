using System.Collections;
using UnityEngine;

public class FrontDoor : MonoBehaviour
{
    public bool woodChopped;
    public bool keypadBroken;
    public bool lockUnlocked;
    public float openDuration = 5f;
    public Transform leftDoor;
    public Transform rightDoor;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        woodChopped = false;
        keypadBroken = false;
        lockUnlocked = false;
    }

    public void chopWood()
    {
        woodChopped = true;
    }

    public void breakKeypad()
    {
        keypadBroken = true;
    }

    public void unlockLock()
    {
        lockUnlocked = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (woodChopped && keypadBroken && lockUnlocked)
        {
            StartCoroutine(openFrontDoors());
        }
    }

//left door rotates to -90
//right door rotates to +90
    IEnumerator openFrontDoors()
    {
        float elapsed = 0f;

        // Store starting rotations
        Quaternion leftStart = leftDoor.localRotation;
        Quaternion rightStart = rightDoor.localRotation;

        // Define target rotations (Local Y axis)
        Quaternion leftTarget = Quaternion.Euler(0, -90f, 0);
        Quaternion rightTarget = Quaternion.Euler(0, 90f, 0);

        while (elapsed < openDuration)
        {
            elapsed += Time.deltaTime;
            
            // Calculate how far along we are (0.0 to 1.0)
            float t = elapsed / openDuration;

            // Apply smooth rotation
            leftDoor.localRotation = Quaternion.Slerp(leftStart, leftTarget, t);
            rightDoor.localRotation = Quaternion.Slerp(rightStart, rightTarget, t);

            yield return null;
        }

        // Final snap to ensure they are perfectly at 90/-90
        leftDoor.localRotation = leftTarget;
        rightDoor.localRotation = rightTarget;
    }
}
