using UnityEngine;

public class AxeTrigger : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.CompareTag("DoorFragment"))
        {
            FragmentBreak fragment = other.gameObject.GetComponent<FragmentBreak>();
            if (fragment != null && !fragment.fragmentBroken)
            {
                Vector3 hitPoint = other.ClosestPoint(transform.position);
                Vector3 forceDirection = (other.transform.position - transform.position).normalized;
                float forceMagnitude = 10f; // Adjust this value to control how hard the fragments are thrown
                Vector3 force = forceDirection * forceMagnitude;
                fragment.breakFragment(force, hitPoint);
            }
        } else if (other.gameObject.CompareTag("DoorPiece"))
        {
            DoorSwap doorSwap = other.gameObject.GetComponent<DoorSwap>();
            if (doorSwap != null)            {
                doorSwap.swapToBrokenModel();
            }
        }
    }
}
