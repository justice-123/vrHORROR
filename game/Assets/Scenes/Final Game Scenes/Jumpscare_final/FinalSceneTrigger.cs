using UnityEngine;

public class JumpscareLoader : MonoBehaviour
{
    private bool hasLoaded = false;

    void OnTriggerEnter(Collider other)
    {
        if (hasLoaded) return;
        if (!other.CompareTag("Player")) return;

        hasLoaded = true;

        // Refill to full — clamp in RefillOxygen handles the cap at 100
        if (OxygenTank.Instance != null)
        {
            OxygenTank.Instance.RefillOxygen(100f);
        }
        else
        {
            Debug.LogWarning("OxygenTank.Instance is null - is CoreSceneMain loaded?");
        }

        Debug.Log("Player crossed loader - loading final_jumpscare");
        MySceneManager.Instance.LoadNewScene("final_jumpscare");
    }
}