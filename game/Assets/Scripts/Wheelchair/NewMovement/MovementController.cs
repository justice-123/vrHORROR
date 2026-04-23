using UnityEngine;

public class MovementController : MonoBehaviour
{

    public Transform head;
    public float currentSpeed;
    public float rotationSpeed;

    public bool movementEnabled;

    CharacterController characterController;

    public Vector3 initialForward;

    public Transform wheelchairModel;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        characterController = GetComponent<CharacterController>();
        initialForward = wheelchairModel.forward;
        movementEnabled = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (!movementEnabled) return;
        Vector3 forward = wheelchairModel.forward;

        forward.y = 0f;
        forward.Normalize();

        Vector3 movement = forward * currentSpeed;
        characterController.Move(movement * Time.deltaTime);
        
    }

    public void RotatePlayer(float degrees)
    {
        if (!movementEnabled) return;
        transform.RotateAround(head.position, Vector3.up, degrees);
        wheelchairModel.rotation = Quaternion.Euler(0, transform.eulerAngles.y, 0);
    }

    public void DisableMovement() {
        movementEnabled = false;
    }

    public void EnableMovement() {
        movementEnabled = true;
    }
}
