using System; // Added for Action event
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class InventoryManager : MonoBehaviour
{

    public static InventoryManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance  = this;
    }

    [Header("References")]
    [SerializeField] private NearFarInteractor handInteractor;
    [SerializeField] private Transform holdPoint;

    [Header("Input")]
    [SerializeField] private InputActionProperty cycleAction;

    private List<InventoryItem> items = new List<InventoryItem>();
    private int currentIndex = -1;
    private GameObject activeInstance = null;

    // Added for tutorial: fired when an item is stored
    public event Action OnItemStored;

    // Added for tutorial: fired when an item is taken out via CycleItem
    public event Action OnItemCycledOut;

    void OnEnable()
    {
        cycleAction.action.Enable();
        cycleAction.action.performed += OnCyclePressed;
        Debug.Log($"[Inventory] Cycle action enabled: {cycleAction.action.enabled}, bindings: {cycleAction.action.bindings.Count}");
    }

    void OnDisable()
    {
        cycleAction.action.performed -= OnCyclePressed;
        cycleAction.action.Disable();
    }

    private void OnCyclePressed(InputAction.CallbackContext ctx)
    {
        Debug.Log("[Inventory] Cycle button pressed!");
        CycleItem();
    }

    public void StoreItem(InventoryItem item)
    {
        if (items.Contains(item)) return;

        var grabInteractable = item.GetComponent<XRGrabInteractable>();
        if (grabInteractable != null && grabInteractable.isSelected)
        {
            handInteractor.interactionManager.CancelInteractorSelection((IXRSelectInteractor)handInteractor);
        }

        items.Add(item);
        item.gameObject.SetActive(false);
        Debug.Log($"[Inventory] Stored: {item.itemName} ({items.Count} items)");

        OnItemStored?.Invoke(); // Added for tutorial
    }

    public void CycleItem()
    {
        if (items.Count == 0) return;

        if (activeInstance != null)
        {
            activeInstance.SetActive(false);
        }

        currentIndex++;
        if (currentIndex >= items.Count)
        {
            currentIndex = -1;
            activeInstance = null;
            Debug.Log("[Inventory] Empty hand");
            return;
        }

        GameObject obj = items[currentIndex].gameObject;
        obj.SetActive(true);
        obj.transform.SetParent(holdPoint);
        obj.transform.localPosition = items[currentIndex].holdOffset;
        obj.transform.localRotation = Quaternion.Euler(items[currentIndex].holdRotation);

        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
        }

        Collider col = obj.GetComponent<BoxCollider>();
        col.isTrigger = true;

        XRGrabInteractable grab = obj.GetComponent<XRGrabInteractable>();
        if (grab != null)
        {
            grab.enabled = false;
        }

        activeInstance = obj;
        Debug.Log($"[Inventory] Equipped: {items[currentIndex].itemName}");

        OnItemCycledOut?.Invoke(); // Added for tutorial
    }

    public void DropCurrentItem()
    {
        if (currentIndex < 0 || currentIndex >= items.Count) return;

        InventoryItem item = items[currentIndex];
        items.RemoveAt(currentIndex);
        currentIndex = -1;

        item.transform.SetParent(null);

        Rigidbody rb = item.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
        }

        XRGrabInteractable grab = item.GetComponent<XRGrabInteractable>();
        if (grab != null)
        {
            grab.enabled = true;
        }

        activeInstance = null;
    }

    public void DeleteCurrentItem()
    {
        if (currentIndex < 0 || currentIndex >= items.Count) return;

        InventoryItem item = items[currentIndex];
        items.RemoveAt(currentIndex);
        currentIndex = -1;
        item.transform.SetParent(null);
        Destroy(activeInstance);

        activeInstance = null;
    }

    public string getActiveItemID()
    {
        return currentIndex != -1 ? items[currentIndex].itemId : "-1";
    }
}