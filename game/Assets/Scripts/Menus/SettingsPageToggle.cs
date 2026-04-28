using UnityEngine;

public class SettingsPageToggle : MonoBehaviour
{
    
    public GameObject mainPage;
    public GameObject settingsPage;

    public void OpenSettings()
    {
        mainPage.SetActive(false);
        settingsPage.SetActive(true);
    }

    public void CloseSettings()
    {
        mainPage.SetActive(true);
        settingsPage.SetActive(false);
    }
}
