using UnityEngine;

public class OxygenRefillStation : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        OxygenTank tank = other.GetComponent<OxygenTank>();
        if (tank != null)
        {
            tank.isRefilling = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        OxygenTank tank = other.GetComponent<OxygenTank>();
        if (tank != null)
        {
            tank.isRefilling = false;
        }
    }
}