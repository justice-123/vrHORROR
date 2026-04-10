using UnityEngine;

public class AxeTrigger : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void OnTriggerEnter(Collider other)
    {
        WoodBreak wood = other.GetComponent<WoodBreak>();

        if (wood != null)
        {
            wood.swapToBrokenModel();
            Debug.Log("chop");
        }
    }
}
