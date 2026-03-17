using UnityEngine;
using UnityEngine.SceneManagement;


public class resetscript : MonoBehaviour
{
    public Transform playerTransform; // XR Origin
    public Transform spawnPoint;

    void OnTriggerEnter(Collider other)
    {

        playerTransform.position = spawnPoint.position;
        playerTransform.rotation = spawnPoint.rotation;
        Physics.SyncTransforms();


        SceneManager.LoadScene(SceneManager.GetActiveScene().name);

    }
}