using UnityEngine;

public class FragmentBreak : MonoBehaviour
{

    public bool fragmentBroken;
    public FragmentBreak[] neighbours;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        fragmentBroken = false;
    }

    public void breakFragment(Vector3 force, Vector3 hitPoint)
    {
        fragmentBroken = true;
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.AddForceAtPosition(force, hitPoint, ForceMode.Impulse);

            Destroy(gameObject, 3f);

            foreach (FragmentBreak neighbour in neighbours)
            {
                if (neighbour != null && !neighbour.fragmentBroken)
                {
                    
                }
            }
        }
    }

    public void checkNeighbours()
    {

        foreach (FragmentBreak neighbour in neighbours)
        {
            if (neighbour != null && !neighbour.fragmentBroken) return;
        }

        fallOff();

    }

    void fallOff()
    {
        if (!fragmentBroken)
        {
            fragmentBroken = true;


            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.useGravity = true;

                Destroy(gameObject, 3f);
            }
        }
    }
}
