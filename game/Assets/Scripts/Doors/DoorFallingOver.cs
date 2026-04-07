using UnityEngine;

public class DoorFallingOver : MonoBehaviour
{
    public Vector3 hitOffset = new Vector3(0, 1f, 0.5f);
    public float pushForce = 8f;

    private Rigidbody rb;
    private bool hasFallen = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (hasFallen || other.tag != "Player") return;

        hasFallen = true;
        rb.isKinematic = false;

        Vector3 force = Vector3.forward * pushForce;

        rb.AddForceAtPosition(force, transform.position + hitOffset, ForceMode.Impulse);
    }

}
