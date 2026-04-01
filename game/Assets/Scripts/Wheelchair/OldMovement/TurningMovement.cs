using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.XR;

public class TurningMovement : MonoBehaviour
{

    // Reference to the MovementController script to update the chair's forward direction
    public MovementController controller;

    // Creates the variables for the actual VR environment, e.g. hands and head
    public Transform rightHandTransform;
    public Transform leftHandTransform;
    UnityEngine.XR.InputDevice rightHand;
    UnityEngine.XR.InputDevice leftHand;
    public XRNode rightHandNode = XRNode.RightHand;
    public XRNode leftHandNode = XRNode.LeftHand;
    public Transform head;

    Vector3 lastPositionR;
    Vector3 lastPositionL;

    public float minimumHandMultiplier = 0.4f;
    public float maximumHandMultiplier = 2.5f;
    public float minimumRotationSpeed = 0.2f;
    public float maximumRotationSpeed = 1.3f;

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

        rightHand.TryGetFeatureValue(UnityEngine.XR.CommonUsages.gripButton, out bool rightGripPressed);
        leftHand.TryGetFeatureValue(UnityEngine.XR.CommonUsages.gripButton, out bool leftGripPressed);


        // Get the direction the chair will be rotating from
        Vector3 chairDirection = controller.wheelchairModel.forward;

        // Initialise a variable for the velocity of the moving hand, and direction of rotation
        Vector3 rotationalVelocity = new Vector3();
        bool rotateClockwise = true;

        // If both pressed, do nothing as this is forward motion
        if (rightGripPressed && leftGripPressed)
        {
            lastPositionR = rightHandTransform.position;
            lastPositionL = leftHandTransform.position;
            return;
        }
        // If no grip pressed, do nothing. For now, rotation will be static, no momentum.
        else if (!rightGripPressed && !leftGripPressed)
        {
            lastPositionR = rightHandTransform.position;
            lastPositionL = leftHandTransform.position;
            return;
        }
        // If right grip pressed, rotate anticlockwise
        else if (rightGripPressed)
        {
            rotationalVelocity = (rightHandTransform.position - lastPositionR) / Mathf.Max(Time.deltaTime, 0.0001f);
            rotateClockwise = false;
        }
        // If left grip pressed, rotate clockwise
        else if (leftGripPressed)
        {
            rotationalVelocity = (leftHandTransform.position - lastPositionL) / Mathf.Max(Time.deltaTime, 0.0001f);
            rotateClockwise = true;
        }

        float rotationDirection = Vector3.Dot(rotationalVelocity, chairDirection);

        float rotationSpeed;

        if (rotationDirection >= 0)
        {
            float t = Mathf.InverseLerp(minimumHandMultiplier, maximumHandMultiplier, rotationDirection);
            rotationSpeed = Mathf.Lerp(minimumRotationSpeed, maximumRotationSpeed, t);
        } else
        {
            float t = Mathf.InverseLerp(-minimumHandMultiplier, -maximumHandMultiplier, rotationDirection);
            rotationSpeed = Mathf.Lerp(-minimumRotationSpeed, -maximumRotationSpeed, t);
        }

        if (rotateClockwise)
        {
            controller.rotationSpeed = rotationSpeed;
        } else
        {
            controller.rotationSpeed = -rotationSpeed;
        }

        // Always keep a record of where the hands are in space
        lastPositionR = rightHandTransform.position;
        lastPositionL = leftHandTransform.position;
    }
}
