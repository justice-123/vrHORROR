// Script by Marcelli Michele

using System.Linq;
using UnityEngine;

public class PadLockPassword : MonoBehaviour
{
    MoveRuller _moveRull;

    public int[] _numberPassword = {0,0,0,0};
    Rigidbody rb;
    public ToiletDoor toiletDoor;
    public Collider mainCollider;

    private void Awake()
    {
        _moveRull = FindFirstObjectByType<MoveRuller>();
        rb = GetComponent<Rigidbody>();
        mainCollider.enabled = false;

    }

    public void Password()
    {
        if (_moveRull._numberArray.SequenceEqual(_numberPassword))
        {
            // Here enter the event for the correct combination
            Debug.Log("Password correct");
            
            toiletDoor.PadlockOpen();
            mainCollider.enabled = true;
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
            }

        }
    }
}
