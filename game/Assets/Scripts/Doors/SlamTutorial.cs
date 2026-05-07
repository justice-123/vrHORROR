using System.Collections;
using System.Numerics;
using UnityEngine;

public class SlamTutorial : MonoBehaviour
{
    public bool slammedTutorial = false;
    public Transform tutorialDoor;

    private void OnTriggerEnter(Collider collider)
    {
        if (!slammedTutorial && collider.CompareTag("Player"))
        {
            slammedTutorial = true;
            StartCoroutine(slamDoor());
        }

    }

    public IEnumerator slamDoor()
    {
        UnityEngine.Vector3 defaulRot = tutorialDoor.eulerAngles;
            UnityEngine.Vector3 openRot = new UnityEngine.Vector3(defaulRot.x, defaulRot.y - 120, defaulRot.z);

            float elapsed = 0f;
        float duration = 1f;

        UnityEngine.Quaternion startRot = UnityEngine.Quaternion.Euler(defaulRot);
        UnityEngine.Quaternion endRot = UnityEngine.Quaternion.Euler(openRot);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            
            // Using Quaternions is safer for rotations to avoid "Gimbal Lock"
            tutorialDoor.rotation = UnityEngine.Quaternion.Slerp(startRot, endRot, elapsed);
            yield return null;

        }

        // Force the final rotation to be exact at the end
        tutorialDoor.rotation = endRot;
    }
}
