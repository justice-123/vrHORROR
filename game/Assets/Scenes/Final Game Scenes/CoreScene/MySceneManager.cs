using UnityEngine;
using UnityEngine.SceneManagement;



public class MySceneManager : MonoBehaviour
{
    public static MySceneManager Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        //LoadNewScene("First Area");
        //LoadNewScene("Lift");

        LoadNewScene("Second Area");

        LoadNewScene("Final-monster-game-scene");
    }

    public void LoadNewScene(string sceneName)
    {
        SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive); // adds scene stuff without deleting rest
    }

    public void UnloadOldScene(string sceneName)
    {
        Debug.Log("Scene Unloading");
        SceneManager.SetActiveScene(SceneManager.GetSceneByName("CoreSceneMain")); 
        SceneManager.UnloadSceneAsync(sceneName);
    }


}
