using UnityEngine;

public class WeaponsCycleRightHand : MonoBehaviour
{
    public GameObject object01;
    public GameObject object02;
    public GameObject object03;

    int currentIndex = 0; // holding nothing

    void Start()
    {
        ApplyWeapon();
    }

    void Update()
    {
        if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch))
        {
            currentIndex++;

            if (currentIndex > 3)
                currentIndex = 0;

            ApplyWeapon();
        }
    }

    void ApplyWeapon()
    {
        object01.SetActive(currentIndex == 1);
        object02.SetActive(currentIndex == 2);
        object03.SetActive(currentIndex == 3);
    }
}
