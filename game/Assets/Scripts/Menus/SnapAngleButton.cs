using TMPro;
using UnityEngine;

public class SnapAngleButton : MonoBehaviour
{
    
    public TextMeshProUGUI buttonText;

    void Start() 
    {
        buttonText.text = PlayerSettings.Instance.snapTurnAngle + "°";
    }

    public void onPress()
    {
        if (PlayerSettings.Instance.snapTurnAngle == 45) PlayerSettings.Instance.SetSnapTurnAngle(22.5f);
        else if (PlayerSettings.Instance.snapTurnAngle == 22.5f) PlayerSettings.Instance.SetSnapTurnAngle(30f);
        else PlayerSettings.Instance.SetSnapTurnAngle(45f);
        buttonText.text = PlayerSettings.Instance.snapTurnAngle + "°";
    }
}
