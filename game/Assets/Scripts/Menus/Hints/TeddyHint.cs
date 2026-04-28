using UnityEngine;

public class TeddyHint : MonoBehaviour
{
    public bool hintTriggered = false;

    public void teddyHint()
    {
        if (!hintTriggered)
        {
            hintTriggered = true;
            HintButton.Instance.changeHintState(HintButton.HintState.TeddyBear);
        }
    }
}
