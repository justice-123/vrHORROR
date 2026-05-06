using UnityEngine;

public class MovementController : MonoBehaviour
{

    public Transform head;
    public float currentSpeed;
    public float rotationSpeed;

    public bool movementEnabled;
    public bool rotated;
    public bool listeningForMovement;

    CharacterController characterController;

    public Vector3 initialForward;

    public Transform wheelchairModel;

    public static MovementController Instance { get; private set; }

 
    void Start()
    {
        Instance = this;
        characterController = GetComponent<CharacterController>();
        initialForward = wheelchairModel.forward;
        movementEnabled = false;
        rotated = false;
    }

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
        if (listeningForMovement)
        {
            rotated = true;
            listeningForMovement = false;
        }
    }

    public void DisableMovement() {
        movementEnabled = false;
    }

    public void EnableMovement() {
        movementEnabled = true;
    }

    public void ListenForMovement()
    {
        listeningForMovement = true;
    }
}
