using System.Collections;
using UnityEngine;

public class AreaTransition : MonoBehaviour
{

    public static AreaTransition Instance { get; private set;}


    public CanvasGroup fadeScreen;
    public Transform player;

    public Vector3 newLiftPosition = new Vector3(-1f, 2.2f, -19.5f);
    public Vector3 newPlayerPosition = new Vector3(-1f, 1.05f, -19.5f);

    public float fadeDuration = 1.5f;

    private void Awake()
    {

        Instance = this;

        fadeScreen.alpha = 0;
    }


    public IEnumerator StartAreaTransition()
    {
        float elapsed  = 0;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            fadeScreen.alpha = elapsed / fadeDuration;
            yield return null;
        }
        fadeScreen.alpha = 1;

        GameObject lift = GameObject.FindWithTag("Lift");

        lift.transform.position = newLiftPosition;
        player.position = newPlayerPosition;

        MySceneManager.Instance.LoadNewScene("Second Area");
        MySceneManager.Instance.UnloadOldScene("First Area");

        yield return new WaitForSeconds(5f);

        elapsed = 0;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            fadeScreen.alpha = 1 - (elapsed / fadeDuration);
            yield return null;
        }
        fadeScreen.alpha = 0;
    }

}
