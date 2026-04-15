using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;

public class SplineEndsFollowObjects : MonoBehaviour
{
    [SerializeField] private SplineContainer splineContainer;
    [SerializeField] private Transform startTarget;
    [SerializeField] private Transform endTarget;

    void LateUpdate()
    {
        if (splineContainer == null || startTarget == null || endTarget == null)
            return;

        Spline spline = splineContainer.Spline;

        if (spline.Count < 2)
            return;

        // Convert world positions into the spline container's local space
        float3 startLocal = splineContainer.transform.InverseTransformPoint(startTarget.position);
        float3 endLocal = splineContainer.transform.InverseTransformPoint(endTarget.position);

        BezierKnot startKnot = spline[0];
        BezierKnot endKnot = spline[spline.Count - 1];

        startKnot.Position = startLocal;
        endKnot.Position = endLocal;

        spline[0] = startKnot;
        spline[spline.Count - 1] = endKnot;
    }
}