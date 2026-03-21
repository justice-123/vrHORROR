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
        tutorialUI.Show("Push LEFT hand forward to turn right");
        pushArrows.leftController  = leftController;
        pushArrows.rightController = null;
        pushArrows.StartArrows();
        yield return StartCoroutine(WaitForPush(both: false, detectLeft: true));  // 直接等推动
        pushArrows.StopArrows();
        PlayDing();

        yield return new WaitForSeconds(1f);

        // ── Part 4: Turn left (right hand forward) ──────
        tutorialUI.Show("Push RIGHT hand forward to turn left");
        pushArrows.leftController  = null;
        pushArrows.rightController = rightController;
        pushArrows.StartArrows();
        yield return StartCoroutine(WaitForPush(both: false, detectLeft: false));  // 直接等推动
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
    // both=true  → 双手都要推
    // both=false, detectLeft=true  → 只检测左手
    // both=false, detectLeft=false → 只检测右手

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
            // 获取设备
            if (leftHand.Count == 0)
                InputDevices.GetDevicesWithCharacteristics(
                    InputDeviceCharacteristics.Left | InputDeviceCharacteristics.Controller,
                    leftHand);
            if (rightHand.Count == 0)
                InputDevices.GetDevicesWithCharacteristics(
                    InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller,
                    rightHand);

            // 检测左手：必须同时握住 Grip 才算推动有效
            if (!leftPushed && leftController != null && leftHand.Count > 0)
            {
                leftHand[0].TryGetFeatureValue(CommonUsages.gripButton, out bool leftGrip);
                if (leftGrip)  // 必须握住
                {
                    Vector3 delta = leftController.position - prevLeft;
                    if (delta.z > threshold) leftPushed = true;
                }
                prevLeft = leftController.position;
            }

            // 检测右手：必须同时握住 Grip 才算推动有效
            if (!rightPushed && rightController != null && rightHand.Count > 0)
            {
                rightHand[0].TryGetFeatureValue(CommonUsages.gripButton, out bool rightGrip);
                if (rightGrip)  // 必须握住
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