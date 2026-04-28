using UnityEngine;

public class GirlHint : MonoBehaviour
{
    public bool hintTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !hintTriggered)
        {
            hintTriggered = true;
            HintButton.Instance.changeHintState(HintButton.HintState.GhostGirl);
        }
    }
}
