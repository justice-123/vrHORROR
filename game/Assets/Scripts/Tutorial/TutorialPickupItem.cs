using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Attach this to an empty GameObject in the scene.
/// On Start it spawns a simple grabbable placeholder item for the grab tutorial.
/// Set spawnPosition in the Inspector to control where it appears.
/// </summary>
public class TutorialPickupItem : MonoBehaviour
{
    [Header("Spawn Settings")]
    public Vector3 spawnPosition = new Vector3(0f, 1.2f, 0.5f);
    public Vector3 spawnRotation = Vector3.zero;

    [Header("Visuals")]
    public Color itemColor = new Color(0.2f, 0.8f, 1f); // bright blue, easy to spot
    public float itemSize  = 0.12f;

    // Reference to the spawned item, used by the tutorial to listen for grabs
    [HideInInspector] public GameObject spawnedItem;

    void Start()
    {
        spawnedItem = CreatePickupItem();
    }

    GameObject CreatePickupItem()
    {
        // Root object
        var go = new GameObject("TutorialItem_Placeholder");
        go.transform.position = spawnPosition;
        go.transform.rotation = Quaternion.Euler(spawnRotation);

        // Visual cube (collider handled separately on the root)
        var visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visual.name = "Visual";
        visual.transform.SetParent(go.transform);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localScale    = Vector3.one * itemSize;

        // Apply colour
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = itemColor;
        visual.GetComponent<Renderer>().material = mat;

        // Remove the collider from the visual; the root has its own
        Destroy(visual.GetComponent<Collider>());

        // Physics
        var col  = go.AddComponent<BoxCollider>();
        col.size = Vector3.one * itemSize;

        var rb            = go.AddComponent<Rigidbody>();
        rb.mass           = 0.3f;
        rb.linearDamping  = 1f;
        rb.angularDamping = 2f;

        // XR grab interaction
        var grab              = go.AddComponent<XRGrabInteractable>();
        grab.movementType     = XRBaseInteractable.MovementType.VelocityTracking;
        grab.throwOnDetach    = false;

        // Inventory data
        var invItem          = go.AddComponent<InventoryItem>();
        invItem.itemName     = "Tutorial Item";
        invItem.itemId       = "tutorial_placeholder";
        invItem.holdOffset   = new Vector3(0f, 0f, 0.05f);
        invItem.holdRotation = Vector3.zero;

        Debug.Log($"[TutorialPickupItem] Spawned at {spawnPosition}");

        return go;
    }

    // Call this to remove the placeholder item after the tutorial finishes
    public void DestroyItem()
    {
        if (spawnedItem != null)
            Destroy(spawnedItem);
    }
}