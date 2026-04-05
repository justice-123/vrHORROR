using System.Collections;
using Oculus.Interaction.Samples;
using UnityEngine;

public class Scanner : MonoBehaviour
{
    
    public InventoryManager inventoryManager;
    public DoorMovement doorMovement;
    public Material lights;
    public AudioSource audioSource;
    private bool doorsOpened = false;

    void Start()
    {
        lights.SetColor("_EmissionColor", Color.blue * 2f);
    }

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

            audioSource.Play();

            doorMovement.openDoors();
            doorsOpened = true;

            inventoryManager.DeleteCurrentItem();

            yield return null;
        }
    }

}
