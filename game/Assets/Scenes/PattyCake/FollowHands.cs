using UnityEngine;

public class FollowHands : MonoBehaviour
{

    public string targetTag = "TargetHand";

    private Transform targetHandBox;

    void Start()
    {
        GameObject targetHand = GameObject.FindGameObjectWithTag(targetTag);

        targetHandBox = targetHand.transform;


    }

    void Update()
    {
        transform.position = targetHandBox.position;

        transform.rotation = targetHandBox.rotation;

    }
}
