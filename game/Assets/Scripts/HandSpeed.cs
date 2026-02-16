using UnityEngine;
using UnityEngine.XR;



public class HandSpeed : MonoBehaviour
{

// Reference to the CircularMotion script to set the speed
    public CircularMotion mover;

// Hand and move speed and rotation thresholds
    public float minimumHandSpeed = 0.4f;
    public float maximumHandSpeed = 2.5f;
    public float minimumMoveSpeed = 0.2f;
    public float maximumMoveSpeed = 1.3f;
    
    public float minimumTurnDegree = 4f;

    // Smoothing factors
    public float smoothing = 10f;
    public float turnSmoothing = 10f;
    public float smoothedSpeed;
    public float smoothedRotR;
    public float smoothedRotL;

    // Persistent direction storing
    Vector3 lastPositionR;
    Vector3 lastPositionL;
    Vector3 previousPlanarVelocityR;
    Vector3 previousPlanarVelocityL;

    // The reference to the head and hand controllers
    public Transform rightHandTransform;
    public Transform leftHandTransform;
    InputDevice rightHand;
    InputDevice leftHand;
    public Transform head;
    public XRNode rightHandNode = XRNode.RightHand;
    public XRNode leftHandNode = XRNode.LeftHand;
    

    // Variables measuring the circularness of the rotations
    public float circleBuildRate = 6f;
    public float circleDecayRate = 3f;
    public float circleThreshold = 0.45f;
    float circleScoreR;
    float circleScoreL;
    float directionalConsistencyR;
    float directionalConsistencyL;
    public float directionalConsistencyThreshold = 0.45f;
    float lockedDirectionR = 0f;
    float lockedDirectionL = 0f;

    //Initialise lastPosition as the current position and attempt to access the right hand controller
    void Start()
    {
        lastPositionR = rightHandTransform.position;
        lastPositionL = leftHandTransform.position;
        rightHand = InputDevices.GetDeviceAtXRNode(rightHandNode);
        leftHand = InputDevices.GetDeviceAtXRNode(leftHandNode);
    }

    void Update()
    {

        // Checks we still have access to the hand
        if (!rightHand.isValid || !leftHand.isValid)
        {
            rightHand = InputDevices.GetDeviceAtXRNode(rightHandNode);
            leftHand = InputDevices.GetDeviceAtXRNode(leftHandNode);
        }

        //checks if grip button is pressed
        rightHand.TryGetFeatureValue(CommonUsages.gripButton, out bool rightGripPressed);
        leftHand.TryGetFeatureValue(CommonUsages .gripButton, out bool leftGripPressed);

        //if not gripping, slow down smoothly and finish the update
        if (!rightGripPressed && !leftGripPressed)
        {
            smoothedSpeed = Mathf.Lerp(smoothedSpeed, 0f, 1f - Mathf.Exp(-smoothing*Time.deltaTime));
            if (mover != null) mover.currentSpeed = smoothedSpeed;
            return;
        }

        if (!leftGripPressed) lastPositionL = leftHandTransform.position;
        if (!rightGripPressed) lastPositionR = rightHandTransform.position;

        // Creates a vector pointing to the right - removes the vertical component of the right of the headset.
        Vector3 sideAxis = Vector3.ProjectOnPlane(head.right, Vector3.up).normalized;

        float targetSpeedL = leftGripPressed ? CalculateLeftHandSpeed(sideAxis) : 0;
        float targetSpeedR = rightGripPressed ? CalculateRightHandSpeed(sideAxis) : 0;

        float targetSpeed = Mathf.Max(targetSpeedL, targetSpeedR);
        // the largest objective speed gets smoothed with its speed in the previous update, makes movement less choppy
        smoothedSpeed = Mathf.Lerp(smoothedSpeed, targetSpeed, 1f - Mathf.Exp(-smoothing * Time.deltaTime));

        //updates the movement controller's speed
        if (mover != null) mover.currentSpeed = smoothedSpeed;

    }

    float CalculateRightHandSpeed(Vector3 sideAxis)
    {
        // Basically speed = distance / time
        Vector3 velocityR = (rightHandTransform.position - lastPositionR) / Mathf.Max(Time.deltaTime, 0.0001f);
        lastPositionR = rightHandTransform.position;

        // Removes the velocity component along wheelAxis
        Vector3 planarVelocityR = Vector3.ProjectOnPlane(velocityR, sideAxis);
        float handSpeedR = planarVelocityR.magnitude;

        float rotationR = 0f;
        if (previousPlanarVelocityR.sqrMagnitude > 0.0001f && planarVelocityR.sqrMagnitude > 0.0001f)
        {
            // if the player is moving, find the angle between their last movement and this one
            rotationR = Vector3.SignedAngle(previousPlanarVelocityR, planarVelocityR, sideAxis);
        }
        float rotationMagnitudeR = Mathf.Abs(rotationR);

        //smooths rotation to try mitigate jittery movement
        smoothedRotR = Mathf.Lerp(smoothedRotR, rotationMagnitudeR, 1f- Mathf.Exp(-turnSmoothing * Time.deltaTime));
        
        // updates which direction the person's hand is moving in
        if (rotationMagnitudeR >= minimumTurnDegree) lockedDirectionR = Mathf.Sign(rotationR);

        // checks if the player is moving in a generally similar direction 
        // this will stop people just shaking their hand about and moving because of that
        directionalConsistencyR = Mathf.Lerp(directionalConsistencyR, lockedDirectionR, 1f - Mathf.Exp(-turnSmoothing * Time.deltaTime));

        //calculates the dot product between the velocities
        //if alignmentR is > 0 then their alignments are less than 90 degrees apart
        bool notReversing = true;
        if (previousPlanarVelocityR.sqrMagnitude > 0.0001f && planarVelocityR.sqrMagnitude > 0.0001f)
        {
            float alignmentR = Vector3.Dot(previousPlanarVelocityR.normalized, planarVelocityR.normalized);
            notReversing = alignmentR > 0.0f;
        }

        
        //there are three conditions for the player to be allowed to move:
        //their arms must be rotating fast enough
        //their arms must be moving fast enough (in any direction)
        //their arms must be moving in a consistent enough direction
        //their arms must not be going back and forwards to try exploit the movement system
        bool turningEnoughR = rotationMagnitudeR >= minimumTurnDegree;
        bool directionConsistentEnoughR = Mathf.Abs(directionalConsistencyR) >= directionalConsistencyThreshold;
        bool circularThisFrameR = turningEnoughR && directionConsistentEnoughR && notReversing && handSpeedR >= minimumHandSpeed;
        
        float currentTime = Time.deltaTime;

        // if a player is moving in a good enough way, they will build up a circle score
        // this circle score will build up as long as they move correctly and will decay if they dont (e.g. start shaking their hand wildly)
        if (circularThisFrameR) circleScoreR = Mathf.Min(1f, circleScoreR + circleBuildRate * currentTime);
        else circleScoreR = Mathf.Max(0f, circleScoreR - circleDecayRate * currentTime);
        

        float targetSpeedR = 0f;
        // if the player is moving 'circularly' enough then we calculate the objective speed they'd be going at without smoothing
        if (circleScoreR >= circleThreshold)
        {
            float t = Mathf.InverseLerp(minimumHandSpeed, maximumHandSpeed, handSpeedR);
            targetSpeedR = Mathf.Lerp(minimumMoveSpeed, maximumMoveSpeed, t);
        }

        previousPlanarVelocityR = planarVelocityR;

        return targetSpeedR;
    }

    float CalculateLeftHandSpeed(Vector3 sideAxis)
    {

        // Basically speed = distance / time
        Vector3 velocityL = (leftHandTransform.position - lastPositionL) / Mathf.Max(Time.deltaTime, 0.0001f);
        lastPositionL = leftHandTransform.position;

        // Removes the velocity component along wheelAxis
        Vector3 planarVelocityL = Vector3.ProjectOnPlane(velocityL, -sideAxis);
        float handSpeedL = planarVelocityL.magnitude;

        float rotationL = 0f;
        if (previousPlanarVelocityL.sqrMagnitude > 0.0001f && planarVelocityL.sqrMagnitude > 0.0001f)
        {
            //if the player is moving, find the angle between thie last movement and this one
            rotationL = Vector3.SignedAngle(previousPlanarVelocityL, planarVelocityL, -sideAxis);
        }
        float rotationMagnitudeL = Mathf.Abs(rotationL);

        //smooths rotation to try mitigate jittery movement
        smoothedRotL = Mathf.Lerp(smoothedRotL, rotationMagnitudeL, 1f- Mathf.Exp(-turnSmoothing * Time.deltaTime));

        // updates which direction the person's hand is moving in
        if (rotationMagnitudeL >= minimumTurnDegree) lockedDirectionL = Mathf.Sign(rotationL);

        // checks if the player is moving in a generally similar direction 
        // this will stop people just shaking their hand about and moving because of that
        directionalConsistencyL = Mathf.Lerp(directionalConsistencyL, lockedDirectionL, 1f - Mathf.Exp(-turnSmoothing * Time.deltaTime));

        //calculates the dot product between the velocities
        //if alignmentL is > 0 then their alignments are less than 90 degrees apart
        bool notReversing = true;
        if (previousPlanarVelocityL.sqrMagnitude > 0.0001f && planarVelocityL.sqrMagnitude > 0.0001f)
        {
            float alignmentL = Vector3.Dot(previousPlanarVelocityL.normalized, planarVelocityL.normalized);
            notReversing = alignmentL > 0.0f;
        }

        //there are three conditions for the player to be allowed to move:
        //their arms must be rotating fast enough
        //their arms must be moving fast enough (in any direction)
        //their arms must be moving in a consistent enough direction
        bool turningEnoughL = rotationMagnitudeL >= minimumTurnDegree;
        bool directionConsistentEnoughL = Mathf.Abs(directionalConsistencyL) >= directionalConsistencyThreshold;
        bool circularThisFrameL = turningEnoughL && directionConsistentEnoughL && notReversing && handSpeedL >= minimumHandSpeed;

        float currentTime = Time.deltaTime;

        // if a player is moving in a good enough way, they will build up a circle score
        // this circle score will build up as long as they move correctly and will decay if they dont (e.g. start shaking their hand wildly)
        if (circularThisFrameL) circleScoreL = Mathf.Min(1f, circleScoreL + circleBuildRate * currentTime);
        else circleScoreL = Mathf.Max(0f, circleScoreL - circleDecayRate * currentTime);

        float targetSpeedL = 0f;
        //if the player is moving their left arm 'circularly' enough then we calculate the objective speed they'd be going at without smoothing
        if (circleScoreL >= circleThreshold)
        {
            float t = Mathf.InverseLerp(minimumHandSpeed, maximumHandSpeed, handSpeedL);
            targetSpeedL = Mathf.Lerp(minimumMoveSpeed, maximumMoveSpeed, t);
        }

        previousPlanarVelocityL = planarVelocityL;

        return targetSpeedL;
    }

   
}
