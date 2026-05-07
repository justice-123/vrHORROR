using UnityEngine;

public class RefillPoint : MonoBehaviour
{
    private GameObject dockedTank;
    private bool hasTank = false;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("StorageTank") && !hasTank)
        {
            // Snap the tank to the refill point
            other.transform.position = transform.position;
            other.transform.rotation = transform.rotation;

            // Parent it so it moves with the point
            other.transform.SetParent(transform);

            // Disable its Rigidbody so it doesn't slide
            Rigidbody rb = other.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true;

            dockedTank = other.gameObject;
            hasTank = true;
        }
    }
}