using UnityEngine;

public class ColliderPieces : MonoBehaviour
{
    

    public float fragmentMass = 0.5f;

    [ContextMenu("Set up colliders")]
    public void SetupColliders()
    {
        foreach(Transform child in transform)
        {
            if (child.GetComponent<Collider>() == null)
            {
                MeshCollider mc = child.gameObject.AddComponent<MeshCollider>();
                mc.convex = true;
            }

            if (child.GetComponent<Rigidbody>() == null)
            {
                Rigidbody rb = child.gameObject.AddComponent<Rigidbody>();
                rb.mass = fragmentMass;
                rb.isKinematic = true;
            }

            if (child.GetComponent<FragmentBreak>() == null)
            {
                child.gameObject.AddComponent<FragmentBreak>();
            }

            child.gameObject.tag = "DoorFragment";
        }
    }

}
