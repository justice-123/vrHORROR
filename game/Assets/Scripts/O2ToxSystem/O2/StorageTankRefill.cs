using UnityEngine;

public class StorageTankRefill : MonoBehaviour
{
    // Tag your oxygen tank GameObject with "OxygenTank"
    private OxygenTank oxygenTank;

    private void OnTriggerEnter(Collider other)
    {
        oxygenTank = other.GetComponent<OxygenTank>();
        if (oxygenTank != null)
        {
            oxygenTank.isRefilling = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        OxygenTank tank = other.GetComponent<OxygenTank>();
        if (tank != null && tank == oxygenTank)
        {
            tank.isRefilling = false;
            oxygenTank = null;
        }
    }
}