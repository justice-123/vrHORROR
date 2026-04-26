using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;

[RequireComponent(typeof(SplineContainer))]
public class OxygenHose : MonoBehaviour
{
    public Transform splinePointTank;
    public Transform splinePointPlayer;

    private SplineContainer splineContainer;
    private Spline spline;

    void Awake()
    {
        splineContainer = GetComponent<SplineContainer>();
        spline = splineContainer.Spline;
    }

    void Update()
    {
        if (splinePointTank == null || splinePointPlayer == null) return;

        Vector3 start = transform.InverseTransformPoint(splinePointTank.position);
        Vector3 end = transform.InverseTransformPoint(splinePointPlayer.position);

        while (spline.Count < 2) spline.Add(new BezierKnot());
        while (spline.Count > 2) spline.RemoveAt(spline.Count - 1);

        spline[0] = new BezierKnot((float3)(Vector3)start);
        spline[1] = new BezierKnot((float3)(Vector3)end);
    }
}