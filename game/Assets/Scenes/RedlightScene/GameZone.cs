using UnityEngine;

public class GameZone : MonoBehaviour
{
    public GameManagerScript gameManager;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) gameManager.SetPlayerInZone(true);
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player")) gameManager.SetPlayerInZone(false);
    }


}
