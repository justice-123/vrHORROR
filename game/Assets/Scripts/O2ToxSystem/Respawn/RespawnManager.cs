using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RespawnManager : MonoBehaviour
{
    public static RespawnManager Instance { get; private set; }

    [Header("Scene Names — must match Build Settings exactly")]
    [SerializeField] private string firstAreaSceneName = "First Area";
    [SerializeField] private string liftSceneName = "Lift";

    [SerializeField] private float fadeDuration = 0.5f;

    public Transform FirstAreaSpawn { get; private set; }
    public Transform SecondAreaSpawn { get; private set; }

    private bool _isRespawning = false;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void RegisterFirstAreaSpawn(Transform t) => FirstAreaSpawn = t;
    public void RegisterSecondAreaSpawn(Transform t) => SecondAreaSpawn = t;

    public void Respawn()
    {
        if (_isRespawning) return;

        string scene = GetCurrentAreaScene();
        VRDebugHUD.Instance?.SetStatus($"Scene: {scene}");

        if (scene == liftSceneName) return;

        _isRespawning = true;
        StartCoroutine(RespawnRoutine(scene));
    }

    /// <summary>
    /// Loops through all loaded scenes and returns the first one that isn't
    /// CoreSceneMain — since areas are loaded additively on top of it.
    /// </summary>
    private string GetCurrentAreaScene()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            string name = SceneManager.GetSceneAt(i).name;
            if (name != "CoreSceneMain")
                return name;
        }
        return SceneManager.GetActiveScene().name;
    }

    private IEnumerator RespawnRoutine(string scene)
    {
        if (AreaTransition.Instance == null)
        {
            VRDebugHUD.Instance?.SetStatus("AreaTransition NULL!");
            _isRespawning = false;
            yield break;
        }

        var fade = AreaTransition.Instance.fadeScreen;
        var player = AreaTransition.Instance.player;

        if (player == null)
        {
            VRDebugHUD.Instance?.SetStatus("AreaTransition.player NULL!");
            _isRespawning = false;
            yield break;
        }

        // Fade to black
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            fade.alpha = Mathf.Clamp01(elapsed / fadeDuration);
            yield return null;
        }
        fade.alpha = 1f;

        // Teleport
        bool isFirstArea = scene == firstAreaSceneName;
        Transform spawnPoint = isFirstArea ? FirstAreaSpawn : SecondAreaSpawn;

        if (spawnPoint == null)
        {
            VRDebugHUD.Instance?.SetStatus($"SPAWN NULL for {scene}!");
        }
        else
        {
            player.position = spawnPoint.position;
            VRDebugHUD.Instance?.SetStatus($"Teleported to {spawnPoint.name}");
        }

        // Set oxygen
        if (OxygenTank.Instance != null)
        {
            OxygenTank.Instance.oxygenLevel = isFirstArea ? 100f : 0f;
            OxygenTank.Instance.isRefilling = false;
        }

        yield return new WaitForSeconds(0.3f);

        // Fade back in
        elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            fade.alpha = Mathf.Clamp01(1f - (elapsed / fadeDuration));
            yield return null;
        }
        fade.alpha = 0f;

        _isRespawning = false;
    }
}