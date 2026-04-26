using UnityEngine;

public class CornerCollision : MonoBehaviour
{

    public Transform[] corners;
    

    // Update is called once per frame
    void Update()
    {
        foreach (Transform corner in corners)
        {
            if (Physics.CheckSphere(corner.position, 0.1f))
            {
                Vector3 pushDirection = transform.position - corner.position;
                pushDirection.y = 0; // Don't push up/down

                // Nudge the player back slightly
                transform.position += pushDirection.normalized * 0.05f;

            }
        }
    }
}
