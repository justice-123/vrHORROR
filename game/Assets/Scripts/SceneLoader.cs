using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public string SceneName = "haptic_vest";

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (!SceneManager.GetSceneByName(SceneName).isLoaded)
            {
                SceneManager.LoadScene(SceneName, LoadSceneMode.Additive);
            }
        }
    }
}