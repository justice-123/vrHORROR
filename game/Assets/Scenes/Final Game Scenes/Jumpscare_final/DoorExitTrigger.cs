using UnityEngine;

public class DoorExitTrigger : MonoBehaviour
{
    public string sceneToUnload = "Second Area"; // unloads current area

    private bool hasTriggered = false;

    void OnTriggerEnter(Collider other)
    {
        if (hasTriggered) return; // prevents firing twice

        if (other.CompareTag("Player"))
        {
            hasTriggered = true;
            MySceneManager.Instance.TransitionToFinalScene(sceneToUnload);
        }
    }
}