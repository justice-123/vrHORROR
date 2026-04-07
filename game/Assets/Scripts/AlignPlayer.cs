using Unity.XR.CoreUtils;
using UnityEngine;

public class AlignPlayer : MonoBehaviour
{

    public XROrigin xrOrigin;
    public Transform cameraTransform;
    public Transform wheelchair;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Invoke(nameof(AlignAndMove), 0.1f);
    }

    void AlignAndMove()
    {
        
        //position
        Vector3 offset = cameraTransform.position - xrOrigin.transform.position;
        offset.y = 0;
        xrOrigin.transform.position = wheelchair.position - offset;

        //rotation
        float rotationAmount = wheelchair.eulerAngles.y - cameraTransform.eulerAngles.y;
        xrOrigin.transform.Rotate(0, rotationAmount, 0);

    }
}
