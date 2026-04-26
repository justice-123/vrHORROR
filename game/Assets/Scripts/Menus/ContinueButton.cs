using UnityEngine;

public class ContinueButton : MonoBehaviour
{
    
    public void PressContinueButton()
    {
        StartCoroutine(MenuTransition.Instance.StartGame());
    }


}
