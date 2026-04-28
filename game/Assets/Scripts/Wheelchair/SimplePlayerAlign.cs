using System.Collections;
using Unity.XR.CoreUtils;
using UnityEngine;

public class SimplePlayerAlign : MonoBehaviour
{

    public XROrigin xrOrigin;
    public Transform head;
    public Transform cameraOffset;
    public CanvasGroup fadeCanvasGroup;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        StartCoroutine(InitialAlign());
    }

    public IEnumerator InitialAlign()
    {

    float snapFadeSpeed = 0.5f;
    float elapsed = 0f;
       while (elapsed <= snapFadeSpeed)
        {
            fadeCanvasGroup.alpha = Mathf.Lerp(0, 1, elapsed / snapFadeSpeed);
            elapsed += Time.deltaTime;
            yield return null;
        }
        fadeCanvasGroup.alpha = 1f;

        head.localPosition = cameraOffset.localPosition;
        head.localRotation = xrOrigin.transform.localRotation;

        elapsed = 0f;
        while (elapsed <= snapFadeSpeed)
        {
            fadeCanvasGroup.alpha = Mathf.Lerp(1, 0, elapsed / snapFadeSpeed);
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
