using UnityEngine;
using UnityEngine.SceneManagement;

public class TestScene : MonoBehaviour
{

    public string sceneToTest = "PattyCake-1";

    void Start()
    {
        SceneManager.LoadSceneAsync(sceneToTest, LoadSceneMode.Additive);
    }

}
