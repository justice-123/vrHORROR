using UnityEngine;

public class InventoryItem : MonoBehaviour
{
    public string itemName = "Item";
    public Vector3 holdOffset = Vector3.zero;
    public Vector3 holdRotation = Vector3.zero;

    [Tooltip("Optional: item ID for game logic checks")]
    public string itemId;
}