using UnityEngine;
using UnityEngine.SceneManagement;

public class GameStartup : MonoBehaviour
{
    [SerializeField] private string firstSceneName = "First Area";
    [SerializeField] private CharacterController characterController;

    private void Start()
    {
        if (characterController != null)
            characterController.enabled = false;

        var op = SceneManager.LoadSceneAsync(firstSceneName, LoadSceneMode.Additive);
        op.completed += OnFirstSceneLoaded;
    }

    private void OnFirstSceneLoaded(AsyncOperation op)
    {
        if (characterController != null)
            characterController.enabled = true;
    }
}