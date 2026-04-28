using UnityEngine;

public class AxeHint : MonoBehaviour
{
    public bool hintTriggered = false;

    public void axeHint()
    {
        if (!hintTriggered)
        {
            hintTriggered = true;
            HintButton.Instance.changeHintState(HintButton.HintState.Axe);
        }
    }
}
