using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class GrabToInventory : MonoBehaviour
{
    [SerializeField] private NearFarInteractor interactor;

    void OnEnable()
    {
        interactor.selectExited.AddListener(OnRelease);
    }

    void OnDisable()
    {
        interactor.selectExited.RemoveListener(OnRelease);
    }

    private void OnRelease(SelectExitEventArgs args)
    {
        InventoryItem item = args.interactableObject.transform.GetComponent<InventoryItem>();
        if (item != null)
        {
            InventoryManager.Instance.StoreItem(item);
        }
    }
}