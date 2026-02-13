using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float mouseSensitivity = 0.1f; 
    float verticalRotation = 0f;


    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    // Update is called once per frame
    void Update()
    {
        // Mouse Look
        Vector2 mouseDelta = Mouse.current.delta.ReadValue() * mouseSensitivity;

        verticalRotation -= mouseDelta.y;
        verticalRotation = Mathf.Clamp(verticalRotation, -90f, 90f);
        Camera.main.transform.localRotation = Quaternion.Euler(verticalRotation, 0, 0);
        transform.Rotate(Vector3.up * mouseDelta.x);

        // Movement (WASD)
        float moveX = 0;
        float moveZ = 0;

        if (Keyboard.current.wKey.isPressed) moveZ = 1;
        if (Keyboard.current.sKey.isPressed) moveZ = -1;
        if (Keyboard.current.aKey.isPressed) moveX = -1;
        if (Keyboard.current.dKey.isPressed) moveX = 1;

        Vector3 move = transform.right * moveX + transform.forward * moveZ;
        transform.position += move.normalized * moveSpeed * Time.deltaTime;

    }
}
