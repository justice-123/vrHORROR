using UnityEngine;

public class CardHint : MonoBehaviour
{
    public bool hintTriggered = false;

    public void triggerHint()
    {
        if (!hintTriggered)
        {
            hintTriggered = true;
            HintButton.Instance.changeHintState(HintButton.HintState.OpenLift);
        }
    }
}
