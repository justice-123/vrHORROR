using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class WireFollow : MonoBehaviour
{
    public Transform startPoint;
    public Transform endPoint;

    public float sagAmount = 0.1f;

    private LineRenderer lr;

    void Start()
    {
        lr = GetComponent<LineRenderer>();
        lr.positionCount = 4;
    }

    void Update()
    {
        if (startPoint == null || endPoint == null) return;

        Vector3 start = startPoint.position;
        Vector3 end = endPoint.position;

        Vector3 dir = end - start;

        // 25% and 75% along the wire
        Vector3 p1 = start + dir * 0.33f;
        Vector3 p2 = start + dir * 0.66f;

        // Add sag (downwards)
        float distance = dir.magnitude;
        float sag = sagAmount * distance;

        p1.y -= sag;
        p2.y -= sag;

        lr.SetPosition(0, start);
        lr.SetPosition(1, p1);
        lr.SetPosition(2, p2);
        lr.SetPosition(3, end);
    }
}