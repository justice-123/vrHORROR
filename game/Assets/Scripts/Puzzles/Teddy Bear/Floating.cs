using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(Rigidbody))]
public class FloatingTeddy : MonoBehaviour
{
    [Header("Bobbing")]
    public float bobHeight = 0.3f;
    public float bobSpeed = 1.5f;

    [Header("Rotation")]
    public float spinSpeed = 45f;

    [Header("Tilt")]
    public float tiltAmount = 15f;
    public float tiltSpeed = 0.8f;

    [Header("Audio")]
    public AudioSource roomAmbienceSource;

    private Vector3 startPos;
    private Rigidbody rb;

    void Start()
    {
        startPos = transform.position;

        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        // Hook into the XR grab event automatically if the component exists
        XRGrabInteractable grabInteractable = GetComponent<XRGrabInteractable>();
        if (grabInteractable != null)
            grabInteractable.selectEntered.AddListener(OnGrabbed);
    }

    void Update()
    {
        // Bobbing on Y axis
        float newY = startPos.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = new Vector3(startPos.x, newY, startPos.z);

        // Continuous Y spin
        transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);

        // Unsettling tilt on X/Z
        float tiltX = Mathf.Sin(Time.time * tiltSpeed * 1.3f) * tiltAmount;
        float tiltZ = Mathf.Cos(Time.time * tiltSpeed) * tiltAmount;
        transform.rotation = Quaternion.Euler(
            transform.rotation.eulerAngles.x + tiltX * Time.deltaTime,
            transform.rotation.eulerAngles.y,
            tiltZ
        );
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        enabled = false;         // stops Update() running, ending all bobbing/spinning
        rb.isKinematic = false;
        rb.useGravity = true;

        if (roomAmbienceSource != null)
            roomAmbienceSource.Stop();
    }

    void OnDestroy()
    {
        // Clean up the listener if the object gets destroyed
        XRGrabInteractable grabInteractable = GetComponent<XRGrabInteractable>();
        if (grabInteractable != null)
            grabInteractable.selectEntered.RemoveListener(OnGrabbed);
    }
}