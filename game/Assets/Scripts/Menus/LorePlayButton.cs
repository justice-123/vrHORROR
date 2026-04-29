using UnityEngine;

public class LorePlayButton : MonoBehaviour
{
    public GameObject mainMenu;
    public GameObject loreMenu;

    public void OnPlayButtonClicked()
    {
        mainMenu.SetActive(false);
        loreMenu.SetActive(true);
    }
}
