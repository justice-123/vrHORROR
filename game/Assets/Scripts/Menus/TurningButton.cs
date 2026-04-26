using UnityEngine;
using UnityEngine.UI;

public class TurningButton : MonoBehaviour
{
    
    public UnityEngine.UI.Button snapButton;
    public UnityEngine.UI.Button smoothButton;

    public Color activeColor = new Color(0, 0.5f, 0);
    public Color inactiveColor = new Color(50f/255f, 50f/255f, 50f/255f);

    public void Start()
    {
        selectSmooth();
    }

    public void selectSnap()
    {
        snapButton.GetComponent<UnityEngine.UI.Image>().color = activeColor;
        smoothButton.GetComponent<UnityEngine.UI.Image>().color = inactiveColor;
        PlayerSettings.Instance.SnapTurning();
    }

    public void selectSmooth()
    {
        snapButton.GetComponent<UnityEngine.UI.Image>().color = inactiveColor;
        smoothButton.GetComponent<UnityEngine.UI.Image>().color = activeColor;
        PlayerSettings.Instance.SmoothTurning();
    }

}
