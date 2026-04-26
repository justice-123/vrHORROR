using System.Collections;
using System.Diagnostics;
using Meta.WitAi.Utilities;
using Microsoft.VisualBasic;
using Oculus.Interaction;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
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
    public float brakingFriction = 0.75f;

    public float maximumMoveSpeed = 2.5f;

    public float pushStrength = 4f;
    public float chairVelocity;
    float pushDeadzone = 0.2f;

    public float turnStrength = 60f;
    float turnDeadzone = 0.6f;

    public Wheelchair_Vignette vignette;

    public enum TurningMethod {Smooth, Snap}
    public TurningMethod methodChosen = TurningMethod.Snap;

    public CanvasGroup blinkerCanvasGroup;
    public float snapAngle = 45f;
    private bool isSnapping = false;
    public float snapFadeSpeed = 0.5f;
    public float snapDeadzone = 0.2f;


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
        
        float forwardImpulse;

        // Only execute the move update if both grip buttons are held
        if (!(rightGripPressed && leftGripPressed))
        {
            chairVelocity *= friction;
            controller.currentSpeed = chairVelocity;

            vignette.currentSpeed = chairVelocity;
            vignette.currentRotationSpeed = 0f;

            //player needs to let go of both grips to snap again
            if (!rightGripPressed && !leftGripPressed) isSnapping = false;

            return;
        }

        // If both grips are pressed
        else 
        {
            //braking
            if (rightHandImpulse == 0f && leftHandImpulse == 0f)
            {
                chairVelocity *= brakingFriction;
                controller.currentSpeed = chairVelocity;

                vignette.currentSpeed = chairVelocity;
                vignette.currentRotationSpeed = 0f;

                return;
            }
            else
            {

                // for forward motion, we double the smallest impulse. this is to punish not moving roughly evenly with both hands.
                if (rightHandImpulse >= 0 && leftHandImpulse >= 0)
                {
                    forwardImpulse = 2 * Mathf.Min(rightHandImpulse, leftHandImpulse);
                }
                else if (rightHandImpulse < 0 && leftHandImpulse < 0)
                {
                    forwardImpulse = 2 * Mathf.Max(rightHandImpulse, leftHandImpulse);
                }
                else
                {
                    forwardImpulse = 0;
                    float direction = (leftHandImpulse > 0) ? 1f : -1f;

                    if (methodChosen == TurningMethod.Smooth) SmoothTurn(leftHandImpulse, rightHandImpulse);
                    else if (!isSnapping && Mathf.Abs(leftHandImpulse) >= snapDeadzone && Mathf.Abs(rightHandImpulse) >= snapDeadzone) StartCoroutine(SnapTurn(direction));
                }
            }

        }

        // increment the chair's velocity by how fast we're going at this current frame
        chairVelocity += forwardImpulse * pushStrength * Time.deltaTime;
        // ensures the player's speed can't go above a certain value
        chairVelocity *= friction;
        chairVelocity = Mathf.Clamp(chairVelocity, -maximumMoveSpeed, maximumMoveSpeed);
        controller.currentSpeed = chairVelocity;

        //updates the vignette script with turning and forward velocities
        vignette.currentSpeed = Mathf.Abs(chairVelocity);
        

    }

    public void SmoothTurn(float leftHandImpulse, float rightHandImpulse)
    {
        //one arm forward and one backward means rotate only
        float minimumMagnitude = Mathf.Min(Mathf.Abs(leftHandImpulse), Mathf.Abs(rightHandImpulse));

        float turnImpulse;
        if (leftHandImpulse > 0)
        {
            turnImpulse = minimumMagnitude * 2f;
        }
        else
        {
            turnImpulse = -minimumMagnitude * 2f;
        }

        // naturally the hands will move at slightly different speeds so we introduce a deadzone to try mitigate accidental turning.
        if (Mathf.Abs(turnImpulse) < turnDeadzone) turnImpulse = 0f;

        float turnSpeed = turnImpulse * turnStrength;
        float rotation = turnSpeed * Time.deltaTime;

        vignette.currentRotationSpeed = Mathf.Abs(turnSpeed);
        // rotates the player by the required amount
        controller.RotatePlayer(rotation);
    }

    public IEnumerator SnapTurn(float direction)
    {
       isSnapping = true;

       float elapsed = 0f;
       while (elapsed <= snapFadeSpeed)
        {
            blinkerCanvasGroup.alpha = Mathf.Lerp(0, 1, elapsed / snapFadeSpeed);
            elapsed += Time.deltaTime;
            yield return null;
        }
        blinkerCanvasGroup.alpha = 1f;

        float currentRotation = transform.eulerAngles.y;
        float targetRotation = Mathf.Round((transform.eulerAngles.y + (45f * direction)) / 45f) * 45f;

        controller.RotatePlayer(Mathf.DeltaAngle(currentRotation, targetRotation));

        yield return new WaitForSeconds(0.05f);

        elapsed = 0f;
        while (elapsed <= snapFadeSpeed)
        {
            blinkerCanvasGroup.alpha = Mathf.Lerp(1, 0, elapsed / snapFadeSpeed);
            elapsed += Time.deltaTime;
            yield return null;
        }
        blinkerCanvasGroup.alpha = 0f;

    }
}
