using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;

public class MainMenuManager : MonoBehaviour
{

    public void LoadTutorialScene()
    {
        // Trigger controller haptic feedback
        XRController controller = FindObjectOfType<XRController>();
        if (controller != null)
        {
            controller.SendHapticImpulse(0.5f, 0.1f);
        }

        // Hide menu
        gameObject.SetActive(false);

        // Load tutorial scene
        SceneManager.LoadScene("Tutorial-2");
    }

 
    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}