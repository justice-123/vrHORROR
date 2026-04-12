using UnityEngine;

public class EscapeTrigger : MonoBehaviour
{
    public FinalSequence finalSequence;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("MainCamera"))
        {
            finalSequence.StartSequence();
        }
    }
}