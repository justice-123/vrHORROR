using UnityEngine;

public class SnapToWheelchair : MonoBehaviour
{
    public Transform snapPoint; // assign O2SnapPoint
    public float snapDistance = 0.2f;

    private bool isSnapped = true;

    void Update()
    {
        if (snapPoint == null) return;

        float distance = Vector3.Distance(transform.position, snapPoint.position);

        if (!isSnapped && distance < snapDistance)
        {
            Snap();
        }
    }

    public void Snap()
    {
        transform.position = snapPoint.position;
        transform.rotation = snapPoint.rotation;

        transform.SetParent(snapPoint);

        isSnapped = true;
    }

    public void Unsnap()
    {
        transform.SetParent(null);
        isSnapped = false;
    }
}