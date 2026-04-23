using UnityEngine;

public class DoorSwap : MonoBehaviour
{
    
    public GameObject brokenObject;

    public void swapToBrokenModel()
    {
        brokenObject.SetActive(true);
        gameObject.SetActive(false);
    }

}
