using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(Rigidbody))]
public class Floating : MonoBehaviour
{
    [Header("Bobbing")]
    public float bobHeight = 0.3f;
    public float bobSpeed = 1.5f;

    [Header("Ritual Light")]
    public Light ritualLight; // drag the child Point Light in here

    [Header("Rotation")]
    public float spinSpeed = 45f;

    [Header("Tilt")]
    public float tiltAmount = 15f;
    public float tiltSpeed = 0.8f;

    [Header("Grab Sequence")]
    public TeddyGrabSequence grabSequence; // drag TeddyGrabSequence object in inspector

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
        enabled = false;
        rb.isKinematic = false;
        rb.useGravity = true;

        if (ritualLight != null)
            ritualLight.enabled = false;

        if (grabSequence != null)
            grabSequence.StartSequence();
    }

    void OnDestroy()
    {
        // Clean up the listener if the object gets destroyed
        XRGrabInteractable grabInteractable = GetComponent<XRGrabInteractable>();
        if (grabInteractable != null)
            grabInteractable.selectEntered.RemoveListener(OnGrabbed);
    }
}