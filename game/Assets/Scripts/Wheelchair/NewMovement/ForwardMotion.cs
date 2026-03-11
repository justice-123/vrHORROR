using Oculus.Interaction;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

public class forwardMotion : MonoBehaviour
{

    // Reference to the MovementController script to set the speed and get the chair direction
    public MovementController controller;

    // Creates the variables for the actual VR environment, e.g. hands and head
    public Transform rightHandTransform;
    public Transform leftHandTransform;
    UnityEngine.XR.InputDevice rightHand;
    UnityEngine.XR.InputDevice leftHand;
    public XRNode rightHandNode = XRNode.RightHand;
    public XRNode leftHandNode = XRNode.LeftHand;
    public Transform head;

    public float smoothedSpeed;
    public float smoothing = 10f;

    Vector3 lastPositionR;
    Vector3 lastPositionL;

    public float minimumHandMultiplier = 0.4f;
    public float maximumHandMuliplier = 2.5f;
    public float minimumMoveSpeed = 0.2f;
    public float maximumMoveSpeed = 1.3f;

    float pushStrength = 3f;
    public float chairVelocity;
    float pushDeadzone = 0.15f;

    public float turnStrength = 60f;
    float turnDeadzone = 0.2f;
    


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
        // Initialise hands 
        rightHand = InputDevices.GetDeviceAtXRNode(rightHandNode);
        leftHand = InputDevices.GetDeviceAtXRNode(leftHandNode);
        chairVelocity = 0f;
    }

    // Update is called once per frame
    void Update()
    {
        
        // Checks we still have access to the hand
        if (!rightHand.isValid ||  !leftHand.isValid)
        {
            rightHand = InputDevices.GetDeviceAtXRNode(rightHandNode);
            leftHand = InputDevices.GetDeviceAtXRNode(leftHandNode);
        }

        //check if grip buttons are pressed
        rightHand.TryGetFeatureValue(UnityEngine.XR.CommonUsages.gripButton, out bool rightGripPressed);
        leftHand.TryGetFeatureValue(UnityEngine.XR.CommonUsages.gripButton, out bool leftGripPressed);

        
        // Only execute the move update if both grip buttons are held
        if (!(rightGripPressed && leftGripPressed))
        {
            chairVelocity *= 0.96f;
            controller.currentSpeed = chairVelocity;
            return;
        }

        else
        {
            // The player only goes forward in the direction of the chair
            Vector3 chairDirection = controller.wheelchairModel.forward;
            Debug.Log(chairDirection);
            chairDirection.y = 0f;
            chairDirection.Normalize();

            // Get each hand's velocity
            rightHand.TryGetFeatureValue(UnityEngine.XR.CommonUsages.deviceVelocity, out Vector3 rightHandVelocity);
            leftHand.TryGetFeatureValue(UnityEngine.XR.CommonUsages.deviceVelocity, out Vector3 leftHandVelocity);

            rightHandVelocity.y = 0f;
            leftHandVelocity.y = 0f;

            //Potentially holdm a rotation matrix???

            //Vector3 rightLocalVelocity = controller.wheelchairModel.InverseTransformDirection(rightHandVelocity);
            //Vector3 leftLocalVelocity = controller.wheelchairModel.InverseTransformDirection(leftHandVelocity);

            // I project the direction of the hand movement onto the direction the chair is facing
            // This should reward the player for pushing in the direction they're facing, and punish them for not
            float rightHandImpulse = Vector3.Dot(rightHandVelocity, chairDirection);
            float leftHandImpulse = Vector3.Dot(leftHandVelocity, chairDirection);
            

            if (rightHandImpulse < pushDeadzone) rightHandImpulse = 0f;
            if (leftHandImpulse < pushDeadzone) leftHandImpulse = 0f;

            float forwardImpulse = 2 * Mathf.Min(rightHandImpulse, leftHandImpulse);

            float turnImpulse = rightHandImpulse - leftHandImpulse;
            Debug.Log("turn impulse:" + turnImpulse);
            if (Mathf.Abs(turnImpulse) < turnDeadzone) turnImpulse = 0f;

            chairVelocity += forwardImpulse * pushStrength * Time.deltaTime;

            float rotation = turnImpulse * turnStrength * Time.deltaTime;
            Debug.Log(rotation);
            

            chairVelocity *= 0.96f;
            chairVelocity = Mathf.Clamp(chairVelocity, 0, maximumMoveSpeed);
            controller.currentSpeed = chairVelocity;

            controller.RotatePlayer(rotation);
        }

    }
}
