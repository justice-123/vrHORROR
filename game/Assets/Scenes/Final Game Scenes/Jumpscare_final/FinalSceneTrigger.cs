using UnityEngine;

public class JumpscareLoader : MonoBehaviour
{
    private bool hasLoaded = false;

    void OnTriggerEnter(Collider other)
    {
        if (hasLoaded) return;
        if (!other.CompareTag("Player")) return;

        hasLoaded = true;
        Debug.Log("Player crossed loader - loading final_jumpscare");
        MySceneManager.Instance.LoadNewScene("final_jumpscare");
    }
}