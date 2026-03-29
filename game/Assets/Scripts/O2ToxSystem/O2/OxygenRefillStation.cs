using UnityEngine;

public class OxygenRefillStation : MonoBehaviour
{
    public RefillCubeVisual visual;

    private void OnTriggerEnter(Collider other)
    {
        OxygenTank tank = other.GetComponent<OxygenTank>();
        if (tank != null)
        {
            tank.isRefilling = true;

            if (visual != null)
                visual.isRefilling = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        OxygenTank tank = other.GetComponent<OxygenTank>();
        if (tank != null)
        {
            tank.isRefilling = false;

            if (visual != null)
                visual.isRefilling = false;
        }
    }
}