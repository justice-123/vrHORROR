using UnityEngine;

public class FollowHands : MonoBehaviour
{

    public string targetTag = "TargetHand";

    private Transform targetHandBox;

    //void Start()
    //{
    //    GameObject targetHand = GameObject.FindGameObjectWithTag(targetTag);

    //    targetHandBox = targetHand.transform;


    //}

    void Update()
    {

        if (targetHandBox == null)
        {
            GameObject targetHand = GameObject.FindGameObjectWithTag(targetTag);

            if (targetHand != null)
            {
               targetHandBox = targetHand.transform;
            }
            else
            {
                return;
            }
        }

        transform.position = targetHandBox.position;

        transform.rotation = targetHandBox.rotation;

    }
}
