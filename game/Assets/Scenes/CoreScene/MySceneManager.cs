using UnityEngine;
using UnityEngine.SceneManagement;



public class MySceneManager : MonoBehaviour
{
    public void LoadNewScene(string sceneName)
    {
        SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive); // adds scene stuff without deleting rest
    }

    public void UnloadOldScene(string sceneName)
    {
        SceneManager.UnloadSceneAsync(sceneName);
    }


}
