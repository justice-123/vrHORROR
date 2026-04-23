using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class WheelchairTutorial : MonoBehaviour
{
    [Header("UI")]
    public TutorialUI tutorialUI;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip successClip;
    public float clipStartTime = 0.3f;

    [Header("Arrow Animation")]
    public PushArrowAnimator pushArrows;
    public Transform leftController;
    public Transform rightController;

    [Header("Grab Tutorial")]
    public InventoryManager inventory;       
    public TutorialPickupItem pickupSpawner; 

    // Flags used by the grab tutorial steps
    bool itemGrabbed = false;
    bool itemStored  = false;
    bool itemCycled  = false;

    void Start() => StartCoroutine(RunTutorial());

    IEnumerator RunTutorial()
    {
        yield return null;

        // Part 1: Grip
        tutorialUI.Show("Hold both Grip buttons to grab wheels");
        yield return StartCoroutine(WaitBothGrips());
        PlayDing();

        yield return new WaitForSeconds(1f);

        // Part 2: Push forward with both hands
        tutorialUI.Show("Push both arms forward to move forward");
        pushArrows.leftController  = leftController;
        pushArrows.rightController = rightController;
        pushArrows.reverseLeftController  = null;
        pushArrows.reverseRightController = null;
        pushArrows.StartArrows();
        yield return StartCoroutine(WaitForPush(both: true));
        pushArrows.StopArrows();
        PlayDing();

        yield return new WaitForSeconds(1f);

        // Part 3: Turn right - left forward, right backward
        tutorialUI.Show("Push LEFT arm forward and RIGHT arm backward to turn RIGHT");
        pushArrows.leftController  = leftController;
        pushArrows.rightController = null;
        pushArrows.reverseLeftController  = null;
        pushArrows.reverseRightController = rightController;
        pushArrows.StartArrows();
        yield return StartCoroutine(WaitForPush(both: false, detectLeft: true));
        pushArrows.StopArrows();
        PlayDing();

        yield return new WaitForSeconds(1f);

        // Part 4: Turn left - right forward, left backward
        tutorialUI.Show("Oppsite direction to turn LEFT");
        pushArrows.leftController  = null;
        pushArrows.rightController = rightController;
        pushArrows.reverseLeftController  = leftController;
        pushArrows.reverseRightController = null;
        pushArrows.StartArrows();
        yield return StartCoroutine(WaitForPush(both: false, detectLeft: false));
        pushArrows.StopArrows();
        PlayDing();

        yield return new WaitForSeconds(1f);

        // Part 5: Breathing info - press A to continue each step
        tutorialUI.ShowWithPrompt("Breath");
        yield return StartCoroutine(WaitForAButton());

        tutorialUI.ShowWithPrompt("Look down to the left of your wheelchair\nthere is an oxygen indicator");
        yield return StartCoroutine(WaitForAButton());

        tutorialUI.ShowWithPrompt("Breathing consumes oxygen\nyou can find refill stations on the map");
        yield return StartCoroutine(WaitForAButton());

        // Part 6: Grab tutorial
        yield return StartCoroutine(RunGrabTutorial());

        // All steps done
        tutorialUI.ShowSuccess();
    }

    // ── Wait for A button (primary button on right controller) ───

    IEnumerator WaitForAButton()
    {
        var rightHand = new List<InputDevice>();

        // Wait for button to be released first, in case it was already held
        bool wasReleased = false;
        while (!wasReleased)
        {
            if (rightHand.Count == 0)
                InputDevices.GetDevicesWithCharacteristics(
                    InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller,
                    rightHand);

            if (rightHand.Count > 0)
            {
                rightHand[0].TryGetFeatureValue(CommonUsages.primaryButton, out bool pressed);
                if (!pressed) wasReleased = true;
            }
            else
            {
                wasReleased = true;
            }

            yield return null;
        }

        // Now wait for a fresh press
        bool confirmed = false;
        while (!confirmed)
        {
            if (rightHand.Count == 0)
                InputDevices.GetDevicesWithCharacteristics(
                    InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller,
                    rightHand);

            if (rightHand.Count > 0)
            {
                rightHand[0].TryGetFeatureValue(CommonUsages.primaryButton, out bool pressed);
                if (pressed) confirmed = true;
            }

            yield return null;
        }
    }

    // ── Grab Tutorial ─────────────────────────────────

    IEnumerator RunGrabTutorial()
    {
        // Reset flags before starting
        itemGrabbed = false;
        itemStored  = false;
        itemCycled  = false;

        // Subscribe to inventory events
        inventory.OnItemStored    += OnItemStored;
        inventory.OnItemCycledOut += OnItemCycledOut;

        // Subscribe to the grab interactable on the spawned item
        XRGrabInteractable grabInteractable = null;
        if (pickupSpawner != null && pickupSpawner.spawnedItem != null)
        {
            grabInteractable = pickupSpawner.spawnedItem.GetComponent<XRGrabInteractable>();
            if (grabInteractable != null)
                grabInteractable.selectEntered.AddListener(_ => itemGrabbed = true);
        }

        // Step 1: Pick up the item
        tutorialUI.Show("All the interactable items have glowing sparkles around");
        yield return new WaitForSeconds(5f);
        tutorialUI.Show("HOLD the Right Trigger to grab the key");
        yield return new WaitUntil(() => itemGrabbed);

        // Stop sparkle particles after pickup
        if (pickupSpawner != null)
            pickupSpawner.StopParticles();

        PlayDing();

        yield return new WaitForSeconds(0.5f);

        // Step 2: Store the item in inventory by releasing trigger
        tutorialUI.Show("Release the button to store it in your inventory");
        yield return new WaitUntil(() => itemStored);
        PlayDing();

        yield return new WaitForSeconds(0.5f);

        // Step 3: Retrieve the item from inventory by pressing A
        tutorialUI.Show("Press A to take it out from your inventory");
        yield return new WaitUntil(() => itemCycled);
        PlayDing();

        yield return new WaitForSeconds(5f);

        // Unsubscribe from all events and clean up
        inventory.OnItemStored    -= OnItemStored;
        inventory.OnItemCycledOut -= OnItemCycledOut;

        if (grabInteractable != null)
            grabInteractable.selectEntered.RemoveAllListeners();

        // Destroy the placeholder item after tutorial is done
        if (pickupSpawner != null)
            pickupSpawner.DestroyItem();
    }

    // Event callbacks for inventory
    void OnItemStored()    => itemStored = true;
    void OnItemCycledOut() => itemCycled = true;

    // ── Wait for both grip buttons to be pressed ──────

    IEnumerator WaitBothGrips()
    {
        bool leftDone  = false;
        bool rightDone = false;
        var leftHand   = new List<InputDevice>();
        var rightHand  = new List<InputDevice>();

        while (!leftDone || !rightDone)
        {
            if (leftHand.Count == 0)
                InputDevices.GetDevicesWithCharacteristics(
                    InputDeviceCharacteristics.Left | InputDeviceCharacteristics.Controller,
                    leftHand);
            if (rightHand.Count == 0)
                InputDevices.GetDevicesWithCharacteristics(
                    InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller,
                    rightHand);

            if (!leftDone && leftHand.Count > 0)
                if (leftHand[0].TryGetFeatureValue(CommonUsages.gripButton, out bool lg) && lg)
                    leftDone = true;

            if (!rightDone && rightHand.Count > 0)
                if (rightHand[0].TryGetFeatureValue(CommonUsages.gripButton, out bool rg) && rg)
                    rightDone = true;

            yield return null;
        }
    }

    // Returns the player's horizontal forward direction based on the headset
    Vector3 GetPlayerForward()
    {
        Vector3 forward = Camera.main.transform.forward;
        forward.y = 0f;
        forward.Normalize();
        return forward;
    }

    // Waits until the relevant hands stop moving
    IEnumerator WaitForHandsToSettle(bool both, bool detectLeft)
    {
        float settleThreshold = 0.002f;
        float settleTime      = 0.1f;
        float stillTimer      = 0f;

        Vector3 prevLeft  = leftController  != null ? leftController.position  : Vector3.zero;
        Vector3 prevRight = rightController != null ? rightController.position : Vector3.zero;

        while (stillTimer < settleTime)
        {
            bool leftNeedsCheck  = both || detectLeft;
            bool rightNeedsCheck = both || !detectLeft;

            bool leftStill  = true;
            bool rightStill = true;

            Vector3 playerForward = GetPlayerForward();

            if (leftNeedsCheck && leftController != null)
            {
                float leftMotion = Mathf.Abs(Vector3.Dot(leftController.position - prevLeft, playerForward));
                leftStill = leftMotion < settleThreshold;
                prevLeft  = leftController.position;
            }

            if (rightNeedsCheck && rightController != null)
            {
                float rightMotion = Mathf.Abs(Vector3.Dot(rightController.position - prevRight, playerForward));
                rightStill = rightMotion < settleThreshold;
                prevRight  = rightController.position;
            }

            if (leftStill && rightStill)
                stillTimer += Time.deltaTime;
            else
                stillTimer = 0f;

            yield return null;
        }
    }

    // Waits for the specified hand(s) to push forward while holding grip
    IEnumerator WaitForPush(bool both, bool detectLeft = true)
    {
        bool leftPushed  = !both && !detectLeft;
        bool rightPushed = !both &&  detectLeft;

        yield return StartCoroutine(WaitForHandsToSettle(both, detectLeft));

        Vector3 prevLeft  = leftController  != null ? leftController.position  : Vector3.zero;
        Vector3 prevRight = rightController != null ? rightController.position : Vector3.zero;

        float threshold = 0.004f;

        var leftHand  = new List<InputDevice>();
        var rightHand = new List<InputDevice>();

        while (!leftPushed || !rightPushed)
        {
            if (leftHand.Count == 0)
                InputDevices.GetDevicesWithCharacteristics(
                    InputDeviceCharacteristics.Left | InputDeviceCharacteristics.Controller,
                    leftHand);
            if (rightHand.Count == 0)
                InputDevices.GetDevicesWithCharacteristics(
                    InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller,
                    rightHand);

            Vector3 playerForward = GetPlayerForward();

            if (!leftPushed && leftController != null && leftHand.Count > 0)
            {
                leftHand[0].TryGetFeatureValue(CommonUsages.gripButton, out bool leftGrip);
                if (leftGrip)
                {
                    Vector3 delta = leftController.position - prevLeft;
                    if (Vector3.Dot(delta, playerForward) > threshold) leftPushed = true;
                }
                prevLeft = leftController.position;
            }

            if (!rightPushed && rightController != null && rightHand.Count > 0)
            {
                rightHand[0].TryGetFeatureValue(CommonUsages.gripButton, out bool rightGrip);
                if (rightGrip)
                {
                    Vector3 delta = rightController.position - prevRight;
                    if (Vector3.Dot(delta, playerForward) > threshold) rightPushed = true;
                }
                prevRight = rightController.position;
            }

            yield return null;
        }
    }

    // ── Play success sound ────────────────────────────

    void PlayDing()
    {
        if (audioSource == null || successClip == null) return;
        audioSource.clip = successClip;
        audioSource.time = clipStartTime;
        audioSource.Play();
    }
}