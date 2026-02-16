using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class CircularMotion : MonoBehaviour
{
    public Transform head;
    public float currentSpeed;

    CharacterController characterController;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
    }

    void Update()
    {
        Vector3 forward = head.forward;
        forward.y = 0f;
        forward.Normalize();

        Vector3 movement = forward * currentSpeed;

        characterController.Move(movement * Time.deltaTime);

    }

    
}
