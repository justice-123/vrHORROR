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


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Initialise last positions as the current position
        lastPositionR = rightHandTransform.position;
        lastPositionL = leftHandTransform.position;
        // Initialise hands 
        rightHand = InputDevices.GetDeviceAtXRNode(rightHandNode);
        leftHand = InputDevices.GetDeviceAtXRNode(leftHandNode);
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
            smoothedSpeed = Mathf.Lerp(smoothedSpeed, 0f, 1f - Mathf.Exp(-smoothing * Time.deltaTime));
            if (controller != null) controller.currentSpeed = smoothedSpeed;
            return;
        }

        else
        {
            // The player only goes forward in the direction of the chair
            Vector3 chairDirection = controller.wheelchairModel.forward;

            // Get each hand's velocity (basically distance / time)
            Vector3 rightHandVelocity = (rightHandTransform.position - lastPositionR) / Mathf.Max(Time.deltaTime, 0.0001f);
            Vector3 leftHandVelocity = (leftHandTransform.position - lastPositionL) / Mathf.Max(Time.deltaTime, 0.0001f);

            // I project the direction of the hand movement onto the direction the chair is facing
            // This should reward the player for pushing in the direction they're facing, and punish them for not
            float rightHandProjection = Vector3.Dot(rightHandVelocity, chairDirection);
            float leftHandProjection = Vector3.Dot(leftHandVelocity, chairDirection);

            // The scalar multiplier for movement speed doubles the minimum hand speed
            // This will punish the player for trying to move fast with one arm only
            float moveSpeedMultiplier = 2 * Mathf.Min(rightHandProjection, leftHandProjection);

            float targetSpeed;

            if (moveSpeedMultiplier >= 0)
            {
                float t = Mathf.InverseLerp(minimumHandMultiplier, maximumHandMuliplier, moveSpeedMultiplier);
                targetSpeed = Mathf.Lerp(minimumMoveSpeed, maximumMoveSpeed, t);
            }

            else
            {
                float t = Mathf.InverseLerp(-minimumHandMultiplier, -maximumHandMuliplier, moveSpeedMultiplier);
                targetSpeed = Mathf.Lerp(-minimumMoveSpeed, -maximumMoveSpeed, t);
            }


            // the largest objective speed gets smoothed with the speed in the previous update
            smoothedSpeed = Mathf.Lerp(smoothedSpeed, targetSpeed, 1f - Mathf.Exp(-smoothing * Time.deltaTime));

            // updates the movement controller's speed
            if (controller != null) controller.currentSpeed = smoothedSpeed;
        }

    }
}
