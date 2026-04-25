using UnityEngine;
using UnityEngine.Splines;

[ExecuteAlways]
public class PipeSpline : MonoBehaviour
{
    public SplineContainer splineContainer;
    public Transform startPoint;
    public Transform endPoint;

    void Update()
    {
        if (splineContainer == null || startPoint == null || endPoint == null) return;

        var spline = splineContainer.Spline;

        if (spline.Count < 2) return;

        spline.SetKnot(0, new BezierKnot(transform.InverseTransformPoint(startPoint.position)));
        spline.SetKnot(spline.Count - 1, new BezierKnot(transform.InverseTransformPoint(endPoint.position)));
    }
}