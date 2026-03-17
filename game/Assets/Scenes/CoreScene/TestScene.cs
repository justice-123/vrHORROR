using UnityEngine;
using UnityEngine.SceneManagement;

public class TestScene : MonoBehaviour
{

    public string sceneToTest = "RedLightGreenLightMain";

    void Start()
    {
        SceneManager.LoadSceneAsync(sceneToTest, LoadSceneMode.Additive);
    }

}
