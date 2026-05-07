using UnityEngine;

public class JumpscareLoader : MonoBehaviour
{
    private bool hasLoaded = false;

    void OnTriggerEnter(Collider other)
    {
        if (hasLoaded) return;
        if (!other.CompareTag("Player")) return;

        hasLoaded = true;
        Debug.Log("loading the final scene- collider has been hit");
        MySceneManager.Instance.LoadNewScene("final_jumpscare");
    }
}