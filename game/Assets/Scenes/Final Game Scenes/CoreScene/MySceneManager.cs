using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MySceneManager : MonoBehaviour
{
    public static MySceneManager Instance { get; private set; }

    // Track scenes that are loading or already loaded to prevent duplicates
    private HashSet<string> _loadingOrLoadedScenes = new HashSet<string>();

    private void Awake()
    {
        // Singleton protection - destroy duplicate instances
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        LoadNewScene("MainMenu");
        LoadNewScene("First Area");
        LoadNewScene("Lift");
    }

    public void LoadNewScene(string sceneName)
    {
        // Check if scene is already loading or loaded
        if (_loadingOrLoadedScenes.Contains(sceneName))
        {
            Debug.LogWarning($"Scene '{sceneName}' is already loaded or loading. Skipping duplicate load.");
            return;
        }

        // Also check if scene already exists in SceneManager
        Scene scene = SceneManager.GetSceneByName(sceneName);
        if (scene.isLoaded)
        {
            Debug.LogWarning($"Scene '{sceneName}' already exists in SceneManager. Skipping.");
            _loadingOrLoadedScenes.Add(sceneName);
            return;
        }

        _loadingOrLoadedScenes.Add(sceneName);
        Debug.Log($"Loading scene: {sceneName}");
        SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
    }

    public void UnloadOldScene(string sceneName)
    {
        Debug.Log("Scene Unloading");
        SceneManager.SetActiveScene(SceneManager.GetSceneByName("CoreSceneMain"));
        SceneManager.UnloadSceneAsync(sceneName);

        // Remove from tracking so it can be loaded again later
        _loadingOrLoadedScenes.Remove(sceneName);
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        DynamicGI.UpdateEnvironment();
        _loadingOrLoadedScenes.Clear();
        SceneManager.LoadScene(0);
    }
}