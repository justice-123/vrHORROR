using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public string SceneName = "haptic_vest";
    public Transform playerRoot;  
    bool loading;

    private void OnTriggerEnter(Collider other)
    {
        if (loading) return;

        if (other.transform.root == playerRoot)
        {
            loading = true;
            SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Additive);
        }
    }
}