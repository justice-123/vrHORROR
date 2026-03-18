using UnityEngine;
using UnityEngine.SceneManagement;

public class TestScene : MonoBehaviour
{

    public string sceneToTest = "Tutorial";

    void Start()
    {
        SceneManager.LoadSceneAsync(sceneToTest, LoadSceneMode.Additive);
    }

}
