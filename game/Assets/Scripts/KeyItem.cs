using UnityEngine;

public class KeyItem : MonoBehaviour
{
    [Tooltip("Must match the lock's requiredKeyId")]
    public string keyId = "BlueKey";

    public bool consumeOnUse = true;

    public void Consume()
    {
        if (consumeOnUse)
            Destroy(gameObject); 
    }
}
