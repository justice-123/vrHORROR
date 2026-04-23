using System.Collections;
using UnityEngine;

public class MenuTransition : MonoBehaviour
{

    public static MenuTransition Instance { get; private set; }

    public CanvasGroup fadeScreen;
    public Transform player;
    public float fadeDuration = 3f;
    public MovementController movementScript;

    private void Awake()
    {
        Instance = this;
        Time.timeScale = 0f;
    }
    
    public IEnumerator StartGame()
    {
        Time.timeScale = 1f;
        float elapsed  = 0;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            fadeScreen.alpha = elapsed / fadeDuration;
            yield return null;
        }
        fadeScreen.alpha = 1;

        player.transform.position = new Vector3(-11, 5, -12);
        MySceneManager.Instance.LoadNewScene("Tutorial");
        yield return new WaitForSeconds(2f);

        elapsed = 0;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            fadeScreen.alpha = 1 - (elapsed / fadeDuration);
            yield return null;
        }
        fadeScreen.alpha = 0;
        
        movementScript.EnableMovement();

    }


}
