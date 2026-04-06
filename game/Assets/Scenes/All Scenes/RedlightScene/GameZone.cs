using UnityEngine;

public class GameZone : MonoBehaviour
{
    public GameManagerScript gameManager;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) gameManager.SetPlayerInZone(true);
        Debug.Log("Player entered the zone.");
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player")) gameManager.SetPlayerInZone(false);
        Debug.Log("Player left the zone.");

    }


}
