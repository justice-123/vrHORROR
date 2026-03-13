using System.Diagnostics;
using Microsoft.VisualBasic;
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

    public float friction = 0.99f;

    public float maximumMoveSpeed = 1.3f;

    float pushStrength = 3f;
    public float chairVelocity;
    float pushDeadzone = 0.2f;

    public float turnStrength = 60f;
    float turnDeadzone = 0.6f;
    


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

        // The player only goes forward in the direction of the chair
        Vector3 chairDirection = controller.wheelchairModel.forward;
        chairDirection.y = 0f;
        chairDirection.Normalize();

        // Get each hand's velocity
        rightHand.TryGetFeatureValue(UnityEngine.XR.CommonUsages.deviceVelocity, out Vector3 rightHandVelocity);
        leftHand.TryGetFeatureValue(UnityEngine.XR.CommonUsages.deviceVelocity, out Vector3 leftHandVelocity);

        // Set the y components to zero just to be safe
        rightHandVelocity.y = 0f;
        leftHandVelocity.y = 0f;

        // Find the angle between where the wheelchair was originally facing forwards and where it is now
        float chairRotationAngle = Vector3.SignedAngle(controller.initialForward, chairDirection, Vector3.up);

        // Rotate the velocity vectors by the previously calculated angle to ensure that movement is the same in all directions
        // Before, the dot products would converge to zero as the chair direction changes but the velocity direction doesnt change much (player movement)
        rightHandVelocity = Quaternion.AngleAxis(chairRotationAngle, Vector3.up) * rightHandVelocity;
        leftHandVelocity = Quaternion.AngleAxis(chairRotationAngle, Vector3.up) * leftHandVelocity;

        // I project the direction of the hand movement onto the direction the chair is facing
        // This should reward the player for pushing in the direction they're facing, and punish them for not
        float rightHandImpulse = Vector3.Dot(rightHandVelocity, chairDirection);
        float leftHandImpulse = Vector3.Dot(leftHandVelocity, chairDirection);

        // We should ignore minor fluctuations in movement, as people cannot stay perfectly still
        if (Mathf.Abs(rightHandImpulse) < pushDeadzone) rightHandImpulse = 0f;
        if (Mathf.Abs(leftHandImpulse) < pushDeadzone) leftHandImpulse = 0f;

        float forwardImpulse = 0f;
        float turnImpulse = 0f;

        
        // Only execute the move update if both grip buttons are held
        if (!rightGripPressed && !leftGripPressed)
        {
            chairVelocity *= friction;
            controller.currentSpeed = chairVelocity;
            return;
        }

        // If only one grip is pressed, only consider the impulse on that grip hand.
        else if (leftGripPressed && !rightGripPressed) turnImpulse = leftHandImpulse;
        else if (rightGripPressed && !leftGripPressed) turnImpulse = -rightHandImpulse;
        
        // If both grips are pressed
        else
        {
            // for forward motion, we double the smallest impulse. this is to punish not moving roughly evenly with both hands.
            forwardImpulse = 2 * Mathf.Min(rightHandImpulse, leftHandImpulse);

            if (rightHandImpulse >= 0 && leftHandImpulse >= 0)
            {
                forwardImpulse = 2 * Mathf.Min(rightHandImpulse, leftHandImpulse);
            } else if (rightHandImpulse < 0 && leftHandImpulse < 0)
            {
                forwardImpulse = 2 * Mathf.Max(rightHandImpulse, leftHandImpulse);
            } else
            {
                forwardImpulse = (rightHandImpulse + leftHandImpulse) / 2;
            }

            // finds the difference between the left and right hand impulses to decide whether to turn or not
            turnImpulse = leftHandImpulse - rightHandImpulse;
            // naturally the hands will move at slightly different speeds so we introduce a deadzone to try mitigate accidental turning.
            if (Mathf.Abs(turnImpulse) < turnDeadzone) turnImpulse = 0f;

            
        }

        // increment the chair's velocity by how fast we're going at this current frame
        chairVelocity += forwardImpulse * pushStrength * Time.deltaTime;
        // do the same with rotation
        float rotation = turnImpulse * turnStrength * Time.deltaTime;
        
        // apply a friction constant to smoothly slow down
        chairVelocity *= friction;

        // ensures the player's speed can't go above a certain value
        chairVelocity = Mathf.Clamp(chairVelocity, -maximumMoveSpeed, maximumMoveSpeed);
        controller.currentSpeed = chairVelocity;

        controller.RotatePlayer(rotation);

    }
}
