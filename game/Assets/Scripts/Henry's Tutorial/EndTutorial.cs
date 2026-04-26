using UnityEngine;

public class EndTutorial : MonoBehaviour
{
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            MySceneManager.Instance.UnloadOldScene("Tutorial");
        }
    }
}
