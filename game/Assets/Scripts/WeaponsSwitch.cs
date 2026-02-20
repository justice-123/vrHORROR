using UnityEngine;

public class WeaponsCycleOnTrigger : MonoBehaviour
{
    [Header("Items in order 0 = none")]
    public GameObject object01;
    public GameObject object02;
    public GameObject object03;

    [Header("Which trigger cycles?")]
    public bool useRightHandTrigger = true;

    [Header("Start weapon (0 = none, 1..3 = objects)")]
    [Range(0, 3)]
    public int startIndex = 0;

    int currentIndex;

    void Start()
    {
        currentIndex = Mathf.Clamp(startIndex, 0, 3);
        ApplyWeapon(currentIndex);
    }

    void Update()
    {
        var controller = useRightHandTrigger ? OVRInput.Controller.RTouch : OVRInput.Controller.LTouch;

        // this activates the moment the button is pressed down rather than firing for every frame its down
        if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, controller))
        {
            // 0 -> 1 -> 2 -> 3 -> 0 -> ...
            currentIndex++;
            if (currentIndex > 3) currentIndex = 0;

            ApplyWeapon(currentIndex);
        }
    }

    void ApplyWeapon(int index)
    {
        if (object01) object01.SetActive(index == 1);
        if (object02) object02.SetActive(index == 2);
        if (object03) object03.SetActive(index == 3);
    }
}
