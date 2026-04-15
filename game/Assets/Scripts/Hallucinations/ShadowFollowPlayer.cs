using UnityEngine;

public class ShadowFollowPlayer : MonoBehaviour
{
    public ToxicityDevice toxicity;
    public Transform player;
    public Animator monsterAnimator;
    public float activateAbove = 40f;
    public float followSpeed = 2f;

    Vector3 startPos;

    void Start()
    {
        if (toxicity == null)
            toxicity = FindAnyObjectByType<ToxicityDevice>();

        if (player == null)
            player = Camera.main.transform;

        startPos = transform.position;

        if (monsterAnimator != null)
            monsterAnimator.enabled = false;
    }

    void Update()
    {
        if (toxicity == null || toxicity.toxicityLevel < activateAbove)
        {
            if (monsterAnimator != null)
                monsterAnimator.enabled = false;
            return;
        }

        if (player == null) return;

        if (monsterAnimator != null)
            monsterAnimator.enabled = true;

        Vector3 target = new Vector3(
            startPos.x,
            startPos.y,
            player.position.z
        );

        transform.position = Vector3.Lerp(
            transform.position, target, followSpeed * Time.deltaTime
        );
    }
}