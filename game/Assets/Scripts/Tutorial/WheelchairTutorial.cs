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

    [Header("Arrow Animation")]
    public PushArrowAnimator pushArrows;
    public Transform leftController;
    public Transform rightController;

    void Start() => StartCoroutine(RunTutorial());

    IEnumerator RunTutorial()
    {
        yield return null;

        // ── first： Grip ──────────────────────
        tutorialUI.Show("hold both Grip buttons to grab both wheels");
        yield return StartCoroutine(WaitBothGrips());
        tutorialUI.Show("Now push forward and move");

        audioSource.clip = successClip;
        audioSource.time = 0.4f;  // ← cut audio
        audioSource.Play();

        yield return new WaitForSeconds(0.5f);

        // ── second：push and arrow animation ───────────────
        pushArrows.leftController  = leftController;
        pushArrows.rightController = rightController;
        pushArrows.StartArrows();

        yield return StartCoroutine(WaitForPush());

        // ── finish  ──────────────────────────────────
        pushArrows.StopArrows();

        audioSource.clip = successClip;
        audioSource.time = 0.4f;  // ← cut audio
        audioSource.Play();

        tutorialUI.ShowSuccess();
    }

    // detect push velocity
    IEnumerator WaitForPush()
    {
        var leftHand  = new List<InputDevice>();
        var rightHand = new List<InputDevice>();

        Vector3 prevLeft  = Vector3.zero;
        Vector3 prevRight = Vector3.zero;
        bool leftPushed   = false;
        bool rightPushed  = false;

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

            
            if (!leftPushed && leftController != null)
            {
                Vector3 delta = leftController.position - prevLeft;
                if (delta.z > 0.004f) leftPushed = true;
                prevLeft = leftController.position;
            }

            
            if (!rightPushed && rightController != null)
            {
                Vector3 delta = rightController.position - prevRight;
                if (delta.z > 0.004f) rightPushed = true;
                prevRight = rightController.position;
            }

            yield return null;
        }
    }

    //detect button
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
}