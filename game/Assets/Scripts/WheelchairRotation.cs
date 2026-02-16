using UnityEngine;

public class WheelchairRotation : MonoBehaviour
{

    //reference to the vr headset
    public Transform head;


    // Update is called once per frame
    void Update()
    {

        if (!head) return;

        //removes the vertical component of the head's position
        Vector3 flatForwardVector = Vector3.ProjectOnPlane(head.forward, Vector3.up);

        //rotate the wheelchair to look exactly where the player is (without rotating up or down thanks to previous line)
        transform.rotation = Quaternion.LookRotation(flatForwardVector.normalized, Vector3.up);
        
    }
}
