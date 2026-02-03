using UnityEngine;

public class GazeVisualiser : MonoBehaviour
{
   
    public Transform centerEyeAnchor;     
    public Transform gazeDot;             
    public float maxDistance = 10f;


    public LayerMask hitMask = ~0;        

    void Reset()
    {
        var rig = FindFirstObjectByType<OVRCameraRig>();
        if (rig != null) centerEyeAnchor = rig.centerEyeAnchor;
    }

    void Update()
    {
        if (centerEyeAnchor == null || gazeDot == null) return;

        Vector3 origin = centerEyeAnchor.position;
        Vector3 dir = centerEyeAnchor.forward;

        if (Physics.Raycast(origin, dir, out RaycastHit hit, maxDistance, hitMask))
        {
            gazeDot.position = hit.point;
        }
        else
        {
            gazeDot.position = origin + dir * maxDistance;
        }
    }
}
