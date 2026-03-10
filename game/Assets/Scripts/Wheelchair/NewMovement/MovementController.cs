using UnityEngine;

public class MovementController : MonoBehaviour
{

    public Transform head;
    public float currentSpeed;
    public Vector3 forward;

    CharacterController characterController;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        characterController = GetComponent<CharacterController>();
        forward = new Vector3(0, 0, 1);
    }

    // Update is called once per frame
    void Update()
    {
        forward.y = 0f;
        forward.Normalize();

        Vector3 movement = forward * currentSpeed;

        characterController.Move(movement * Time.deltaTime);
    }
}
