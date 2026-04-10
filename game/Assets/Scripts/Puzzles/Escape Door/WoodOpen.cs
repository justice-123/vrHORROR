using UnityEngine;

public class WoodOpen : MonoBehaviour
{
    public WoodBreak wood1;
    public WoodBreak wood2;
    public FrontDoor frontDoor;


    // Update is called once per frame
    void Update()
    {
        if (wood1.woodBroken && wood2.woodBroken)
        {
            frontDoor.chopWood();
        }
    }
}
