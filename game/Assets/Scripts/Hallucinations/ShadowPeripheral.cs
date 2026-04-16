using UnityEngine;

public class ShadowPeripheral : MonoBehaviour
{
    public ToxicityDevice toxicity;
    public Light spotLight;
    public Transform monsterModel;
    public Animator monsterAnimator;
    public float activateAbove = 40f;
    public float followSpeed = 2f;
    public float fleeSpeed = 8f;
    public float hideAngle = 30f;

    [Header("Distortion")]
    public float distortSpeed = 1.5f;
    public float distortAmount = 0.3f;

    Transform player;
    Vector3 startPos;
    Vector3 baseScale;
    float baseIntensity;
    float sideSign = 1f;
    float flipCooldown;

    void Start()
    {
        if (toxicity == null)
            toxicity = FindAnyObjectByType<ToxicityDevice>();

        player = Camera.main.transform;
        startPos = transform.position;
        baseIntensity = spotLight.intensity;
        baseScale = monsterModel.localScale;
        spotLight.enabled = false;

        if (monsterAnimator != null)
            monsterAnimator.enabled = false;
    }

    void Update()
    {
        if (toxicity == null || toxicity.toxicityLevel < activateAbove)
        {
            spotLight.enabled = false;
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

        flipCooldown -= Time.deltaTime;

        if (angle < hideAngle)
        {
            if (flipCooldown <= 0f)
            {
                sideSign *= -1f;
                flipCooldown = 1f;
            }
            spotLight.enabled = false;
            if (monsterAnimator != null)
                monsterAnimator.enabled = false;
            return;
        }

        spotLight.enabled = true;
        if (monsterAnimator != null)
            monsterAnimator.enabled = true;

        // follow player along wall, offset to one side
        float offset = sideSign * 3f;
        Vector3 target = new Vector3(
            startPos.x,
            startPos.y,
            player.position.z + offset
        );
        transform.position = Vector3.Lerp(
            transform.position, target, followSpeed * Time.deltaTime
        );

        // distort the monster shape
        float t = (toxicity.toxicityLevel - activateAbove)
                / (100f - activateAbove);
        float time = Time.time * distortSpeed;
        float scaleX = 1f + Mathf.Sin(time) * distortAmount * t;
        float scaleY = 1f + Mathf.Sin(time * 1.3f) * distortAmount * 0.5f * t;
        float scaleZ = 1f + Mathf.Sin(time * 0.7f) * distortAmount * t;
        monsterModel.localScale = new Vector3(
            baseScale.x * scaleX,
            baseScale.y * scaleY,
            baseScale.z * scaleZ
        );
    }
}