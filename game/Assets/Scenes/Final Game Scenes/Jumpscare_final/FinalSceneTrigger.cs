using UnityEngine;

public class FinalSceneTrigger : MonoBehaviour
{
    public string sceneToUnload = "Second Area";
    private bool hasTriggered = false;

    void OnTriggerEnter(Collider other)
    {
        Debug.Log("Trigger entered by: " + other.name + " with tag: " + other.tag);

        if (hasTriggered) return;

        if (other.CompareTag("Player"))
        {
            hasTriggered = true;
            Debug.Log("Player crossed doorway - loading final_jumpscare!");
            MySceneManager.Instance.TransitionToFinalScene(sceneToUnload);
        }
    }
}