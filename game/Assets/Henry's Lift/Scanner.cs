using System.Collections;
using UnityEngine;

public class Scanner : MonoBehaviour
{
    
    public InventoryManager inventoryManager;
    public DoorMovement doorMovement;
    public Material lights;
    private bool doorsOpened = false;

    private void OnTriggerEnter(Collider box)
    {
        if (!doorsOpened)
        {
            StartCoroutine(cardCheck());
        }
    }

    public IEnumerator cardCheck()
    {
        if (inventoryManager.getActiveItemID() == "liftcard")
        {
            lights.SetColor("_EmissionColor", Color.green * 1.5f);

            doorMovement.openDoors();
            doorsOpened = true;

            yield return new WaitForSeconds(2f);

            inventoryManager.DeleteCurrentItem();
        }
    }

}
