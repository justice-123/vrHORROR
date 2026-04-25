using UnityEngine;

public class PlayButton : MonoBehaviour
{
    public GameObject mainPage;
    public GameObject lorePage;

    public void OnPlayButton()
    {
        mainPage.SetActive(false);
        lorePage.SetActive(true);
    }
}
