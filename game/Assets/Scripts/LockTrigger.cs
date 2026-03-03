using UnityEngine;

public class LockTrigger : MonoBehaviour
{
    public string requiredKeyId = "BlueKey";
    public DoorVR door;
    public Transform snapPoint; // optional

    private bool unlocked = false;

    private void OnTriggerEnter(Collider other)
    {
        if (unlocked) return;
        if (door == null) return;

        KeyItem key = other.GetComponentInParent<KeyItem>();
        if (key == null) return;

        if (key.keyId != requiredKeyId) return;

        unlocked = true;

        if (snapPoint != null)
        {
            key.transform.SetPositionAndRotation(snapPoint.position, snapPoint.rotation);

            var rb = key.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
            }
        }

        door.UnlockAndOpen();
        key.Consume();
    }
}
