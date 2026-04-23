using UnityEngine;
using UnityEngine.InputSystem;

public class TogglePauseMenu : MonoBehaviour
{

    public InputActionReference menuAction;
    public GameObject pauseMenu;
    public MovementController movementScript;
    
    private void OnEnable()
    {
        menuAction.action.Enable();
        menuAction.action.performed += OnMenuPress;
    }

    private void OnDisable()
    {
        menuAction.action.performed -= OnMenuPress;
    }

    private void OnMenuPress(InputAction.CallbackContext context)
    {
        Toggle();
    }

    public void Toggle()
    {
        bool isPausing = !pauseMenu.activeSelf;

        pauseMenu.SetActive(isPausing);
        Time.timeScale = isPausing ? 0f : 1f;

        if (isPausing)
        {
            movementScript.DisableMovement();
        }
        else
        {
            movementScript.EnableMovement();
        }
    }

    public void RestartGame()
    {
        MySceneManager.Instance.RestartGame();
    }

}
