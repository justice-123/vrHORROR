using UnityEngine;

public class LampHint : MonoBehaviour
{
    public bool triggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !triggered)
        {
            triggered = true;
            HintButton.Instance.changeHintState(HintButton.HintState.LampPuzzle);
        }
    }
}
