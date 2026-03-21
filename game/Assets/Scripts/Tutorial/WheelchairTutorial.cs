using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

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

    void Start() => StartCoroutine(RunTutorial());

    IEnumerator RunTutorial()
    {
        yield return null;

        // ── Part 1: Grip ───────────────────────────────
        tutorialUI.Show("Hold both Grip buttons to grab both wheels");
        yield return StartCoroutine(WaitBothGrips());
        PlayDing();

        yield return new WaitForSeconds(1f);

        // ── Part 2: Push forward (both hands) ──────────
        tutorialUI.Show("Push both hands forward to move");
        pushArrows.leftController  = leftController;
        pushArrows.rightController = rightController;
        pushArrows.StartArrows();
        yield return StartCoroutine(WaitForPush(both: true));
        pushArrows.StopArrows();
        PlayDing();

        yield return new WaitForSeconds(1f);

        // ── Part 3: Turn right (left hand forward) ──────
        tutorialUI.Show("Push LEFT arm forward to turn right");
        pushArrows.leftController  = leftController;
        pushArrows.rightController = null;
        pushArrows.StartArrows();
        yield return StartCoroutine(WaitForPush(both: false, detectLeft: true));  
        pushArrows.StopArrows();
        PlayDing();

        yield return new WaitForSeconds(1f);

        // ── Part 4: Turn left (right hand forward) ──────
        tutorialUI.Show("Push RIGHT arm forward to turn left");
        pushArrows.leftController  = null;
        pushArrows.rightController = rightController;
        pushArrows.StartArrows();
        yield return StartCoroutine(WaitForPush(both: false, detectLeft: false));  
        pushArrows.StopArrows();
        PlayDing();

        yield return new WaitForSeconds(0.5f);

        // ── Complete ────────────────────────────────────
        tutorialUI.ShowSuccess();
    }
    // ── Detect both grips pressed ─────────────────────

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

    // ── Detect forward push ───────────────────────────
    // both=true  → both arms
    // both=false, detectLeft=true  → left arm
    // both=false, detectLeft=false → right arm

    IEnumerator WaitForPush(bool both, bool detectLeft = true)
    {
        bool leftPushed  = !both && !detectLeft;
        bool rightPushed = !both &&  detectLeft;

        Vector3 prevLeft  = leftController  != null ? leftController.position  : Vector3.zero;
        Vector3 prevRight = rightController != null ? rightController.position : Vector3.zero;

        float threshold = 0.004f;

        var leftHand  = new List<InputDevice>();
        var rightHand = new List<InputDevice>();

        while (!leftPushed || !rightPushed)
        {
            // get VR device
            if (leftHand.Count == 0)
                InputDevices.GetDevicesWithCharacteristics(
                    InputDeviceCharacteristics.Left | InputDeviceCharacteristics.Controller,
                    leftHand);
            if (rightHand.Count == 0)
                InputDevices.GetDevicesWithCharacteristics(
                    InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller,
                    rightHand);

            // detect left arm : should hold grip button
            if (!leftPushed && leftController != null && leftHand.Count > 0)
            {
                leftHand[0].TryGetFeatureValue(CommonUsages.gripButton, out bool leftGrip);
                if (leftGrip)  // should hold
                {
                    Vector3 delta = leftController.position - prevLeft;
                    if (delta.z > threshold) leftPushed = true;
                }
                prevLeft = leftController.position;
            }

            // detect right arm : should hold grip button
            if (!rightPushed && rightController != null && rightHand.Count > 0)
            {
                rightHand[0].TryGetFeatureValue(CommonUsages.gripButton, out bool rightGrip);
                if (rightGrip)  // should hold
                {
                    Vector3 delta = rightController.position - prevRight;
                    if (delta.z > threshold) rightPushed = true;
                }
                prevRight = rightController.position;
            }

            yield return null;
        }
    }

    // ── Play ding ─────────────────────────────────────

    void PlayDing()
    {
        if (audioSource == null || successClip == null) return;
        audioSource.clip = successClip;
        audioSource.time = clipStartTime;
        audioSource.Play();
    }
}