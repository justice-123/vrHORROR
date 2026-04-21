using System.Collections;
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
        
         LoadNewScene("First Area");
         LoadNewScene("Lift");

         
         LoadNewScene("final_jumpscare");
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

    public void TransitionToFinalScene(string sceneToUnload)
    {
        StartCoroutine(LoadFinalSceneAdditive());
    }

    private IEnumerator LoadFinalSceneAdditive()
    {
        Debug.Log("Loading final_jumpscare on top of Second Area");

        AsyncOperation load = SceneManager.LoadSceneAsync("final_jumpscare", LoadSceneMode.Additive);

        while (!load.isDone)
        {
            yield return null;
        }

        Debug.Log("final_jumpscare loaded successfully!");
    }
}