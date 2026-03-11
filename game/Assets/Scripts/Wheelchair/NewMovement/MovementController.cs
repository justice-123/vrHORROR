using UnityEngine;

public class MovementController : MonoBehaviour
{

    public Transform head;
    public float currentSpeed;
    public float rotationSpeed;

    CharacterController characterController;

    public Transform wheelchairModel;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        characterController = GetComponent<CharacterController>();
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 forward = wheelchairModel.forward;

        forward.y = 0f;
        forward.Normalize();

        Vector3 movement = forward * currentSpeed;
        characterController.Move(movement * Time.deltaTime);
        
    }

    public void RotatePlayer(float degrees)
    {
        transform.RotateAround(head.position, Vector3.up, degrees);
        wheelchairModel.rotation = Quaternion.Euler(0, transform.eulerAngles.y, 0);
    }
}
