using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MySceneManager : MonoBehaviour
{
    public static MySceneManager Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        LoadNewScene("MainMenu");
        //LoadNewScene("final_jumpscare");
        LoadNewScene("First Area");
        LoadNewScene("Lift");
    }

    public void LoadNewScene(string sceneName)
    {
        SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
    }
    
    public void UnloadOldScene(string sceneName)
    {
        Debug.Log("Scene Unloading");
        SceneManager.SetActiveScene(SceneManager.GetSceneByName("CoreSceneMain"));
        SceneManager.UnloadSceneAsync(sceneName);
    }

}