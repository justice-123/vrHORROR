using UnityEngine;

public class PlayButton : MonoBehaviour
{
    
    public void PressButton()
    {
        StartCoroutine(MenuTransition.Instance.StartGame());
    }


}
