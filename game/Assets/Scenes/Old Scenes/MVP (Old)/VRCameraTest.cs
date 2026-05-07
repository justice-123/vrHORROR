using UnityEngine;

public class VRCameraTest : MonoBehaviour
{
    [Header("Test Movement Settings")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float lookSpeed = 2f;
    [SerializeField] private bool enableTestControls = true;

    private Transform cameraTransform;

    void Start()
    {
        // Find the VR camera
        Camera cam = GetComponentInChildren<Camera>();
        if (cam != null)
        {
            cameraTransform = cam.transform;
            
        }
    }

    void Update()
    {
        // Only work in editor when no headset connected
        if (enableTestControls && !OVRManager.isHmdPresent)
        {
            HandleMovement();
            HandleRotation();
        }
    }

    void HandleMovement()
    {
        // WASD movement
        float horizontal = Input.GetAxis("Horizontal"); // A/D or Left/Right arrows
        float vertical = Input.GetAxis("Vertical");     // W/S or Up/Down arrows

        // Move relative to where you're looking
        Vector3 forward = cameraTransform.forward;
        forward.y = 0; // Keep movement horizontal
        forward.Normalize();

        Vector3 right = cameraTransform.right;
        right.y = 0;
        right.Normalize();

        Vector3 movement = (forward * vertical + right * horizontal) * moveSpeed * Time.deltaTime;
        transform.position += movement;

        // Q/E for vertical movement
        if (Input.GetKey(KeyCode.Q))
            transform.position += Vector3.down * moveSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.E))
            transform.position += Vector3.up * moveSpeed * Time.deltaTime;
    }

    void HandleRotation()
    {
        // Right-click and drag to look around
        if (Input.GetMouseButton(1))
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            float mouseX = Input.GetAxis("Mouse X") * lookSpeed;
            float mouseY = Input.GetAxis("Mouse Y") * lookSpeed;

            // Rotate body left/right
            transform.Rotate(0, mouseX, 0, Space.World);

            // Rotate camera up/down
            Vector3 currentRotation = cameraTransform.localEulerAngles;
            currentRotation.x -= mouseY;

            // Clamp vertical rotation
            if (currentRotation.x > 180)
                currentRotation.x -= 360;
            currentRotation.x = Mathf.Clamp(currentRotation.x, -80, 80);

            cameraTransform.localEulerAngles = new Vector3(currentRotation.x, cameraTransform.localEulerAngles.y, 0);
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}