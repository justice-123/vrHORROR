using System.Collections;
using NUnit.Framework;
using Unity.XR.CoreUtils;
using UnityEditor.XR.LegacyInputHelpers;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using UnityEngine.XR;

public class AlignPlayer : MonoBehaviour
{

    public XROrigin xrOrigin;
    public Transform cameraTransform;
    public Transform wheelchair;
    public Transform cameraOffset;
    private bool _wasUserPresent = false;

  
    void Start()
    {
        StartCoroutine(InitialAlign());
    }

    void Update()
    {
        CheckUserPresence();
    }

    void CheckUserPresence()
    {
        UnityEngine.XR.InputDevice device = InputDevices.GetDeviceAtXRNode(XRNode.CenterEye);

        if (device.isValid)
        {
            if (device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.userPresence, out bool isUserPresent))
            {
                if (isUserPresent && !_wasUserPresent)
                {
                    AlignAndMove(false);
                }

                _wasUserPresent = isUserPresent;
            }
        }
    }

    IEnumerator InitialAlign()
    {

        while(cameraTransform.localPosition == Vector3.zero)
        {
            yield return null;
        }

        yield return new WaitForSeconds(0.2f);

        AlignAndMove(true);
    }

    void AlignAndMove(bool snapTo45)
    {
        float targetYaw = wheelchair.eulerAngles.y;
        if (snapTo45) targetYaw = Mathf.Round(targetYaw / 45f) * 45f;

        float rotationAmount = targetYaw - cameraTransform.eulerAngles.y;
        xrOrigin.transform.Rotate(0, rotationAmount, 0);

        Vector3 cameraToOrigin = xrOrigin.transform.position - cameraTransform.position;
        xrOrigin.transform.position = cameraOffset.position + cameraToOrigin;

    }
}
