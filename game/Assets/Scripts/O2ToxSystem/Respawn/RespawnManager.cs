using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RespawnManager : MonoBehaviour
{
    public static RespawnManager Instance { get; private set; }

    [Header("Scene Names — must match Build Settings exactly")]
    [SerializeField] private string firstAreaSceneName = "First Area";

    [SerializeField] private float fadeDuration = 0.5f;

    [Header("Death Audio")]
    [SerializeField] private AudioSource tinnitusAudio;
    [SerializeField] private float audioFadeDuration = 0.5f;

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

        bool inFirstArea = false;
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            if (SceneManager.GetSceneAt(i).name == firstAreaSceneName)
            {
                inFirstArea = true;
                break;
            }
        }

        VRDebugHUD.Instance?.SetStatus($"Respawning - FirstArea: {inFirstArea}");

        _isRespawning = true;
        StartCoroutine(RespawnRoutine(inFirstArea));
    }

    private IEnumerator RespawnRoutine(bool isFirstArea)
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

        // Fade screen to black
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            fade.alpha = Mathf.Clamp01(elapsed / fadeDuration);
            yield return null;
        }
        fade.alpha = 1f;

        // Fade tinnitus in
        if (tinnitusAudio != null)
        {
            tinnitusAudio.volume = 0f;
            tinnitusAudio.Play();
            elapsed = 0f;
            while (elapsed < audioFadeDuration)
            {
                elapsed += Time.deltaTime;
                tinnitusAudio.volume = Mathf.Clamp01(elapsed / audioFadeDuration);
                yield return null;
            }
            tinnitusAudio.volume = 1f;
        }

        // Teleport
        Transform spawnPoint = isFirstArea ? FirstAreaSpawn : SecondAreaSpawn;
        if (spawnPoint == null)
            VRDebugHUD.Instance?.SetStatus($"SPAWN NULL - isFirstArea:{isFirstArea}");
        else
        {
            player.position = spawnPoint.position;
            VRDebugHUD.Instance?.SetStatus($"Teleported to {spawnPoint.name}");
        }

        if (OxygenTank.Instance != null)
        {
            OxygenTank.Instance.oxygenLevel = isFirstArea ? 100f : 10f;
            OxygenTank.Instance.isRefilling = false;
        }

        // Hold in darkness
        yield return new WaitForSeconds(2.3f);

        // Fade tinnitus out
        if (tinnitusAudio != null)
        {
            elapsed = 0f;
            while (elapsed < audioFadeDuration)
            {
                elapsed += Time.deltaTime;
                tinnitusAudio.volume = Mathf.Clamp01(1f - (elapsed / audioFadeDuration));
                yield return null;
            }
            tinnitusAudio.volume = 0f;
            tinnitusAudio.Stop();
        }

        // Fade screen back in
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