using UnityEngine;

public class ShadowPeripheral : MonoBehaviour
{
    public ToxicityDevice toxicity;
    public Transform player;
    public Animator monsterAnimator;
    public float activateAbove = 40f;
    public float followSpeed = 2f;
    public float peripheralAngle = 60f;
    public float hideAngle = 30f;

    Vector3 startPos;
    bool visible = true;

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

        Vector3 toShadow = transform.position - player.position;
        toShadow.y = 0f;

        Vector3 playerForward = player.forward;
        playerForward.y = 0f;
        float angle = Vector3.Angle(playerForward, toShadow);

        if (angle < hideAngle)
        {
            if (monsterAnimator != null)
                monsterAnimator.enabled = false;
            return;
        }

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