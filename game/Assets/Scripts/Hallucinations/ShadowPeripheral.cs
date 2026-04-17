using UnityEngine;

public class ShadowPeripheral : MonoBehaviour
{
    public ToxicityDevice toxicity;
    public Light spotLight;
    public Transform monsterModel;
    public Animator monsterAnimator;
    public float activateAbove = 40f;
    public float followSpeed = 8f;
    public float hideAngle = 30f;
    public float fadeSpeed = 3f;

    [Header("Distortion")]
    public float distortSpeed = 1.5f;
    public float distortAmount = 0.3f;

    [Header("Audio")]
    public AudioSource crawlSource;
    public AudioClip crawlClip;

    Transform player;
    Vector3 startPos;
    Vector3 baseScale;
    float baseIntensity;
    float currentIntensity;
    bool wasVisible;

    void Start()
    {
        if (toxicity == null)
            toxicity = FindAnyObjectByType<ToxicityDevice>();

        player = Camera.main.transform;
        startPos = transform.position;
        baseScale = monsterModel.localScale;
        baseIntensity = spotLight.intensity;
        currentIntensity = 0f;
        spotLight.intensity = 0f;

        if (monsterAnimator != null)
            monsterAnimator.enabled = false;
    }

    void Update()
    {
        if (toxicity == null || toxicity.toxicityLevel < activateAbove)
        {
            currentIntensity = Mathf.MoveTowards(currentIntensity, 0f, fadeSpeed * Time.deltaTime);
            spotLight.intensity = currentIntensity;
            if (currentIntensity <= 0f)
            {
                if (monsterAnimator != null)
                    monsterAnimator.enabled = false;
                if (crawlSource != null && crawlSource.isPlaying)
                    crawlSource.Stop();
            }
            wasVisible = false;
            return;
        }

        if (player == null) return;

        int layerMask = ~LayerMask.GetMask("ShadowOnly");
        Vector3 shadowPoint;
        RaycastHit hit;
        if (Physics.Raycast(spotLight.transform.position, spotLight.transform.forward, out hit, Mathf.Infinity, layerMask))
            shadowPoint = hit.point;
        else
            shadowPoint = spotLight.transform.position + spotLight.transform.forward * 5f;

        Vector3 toShadow = shadowPoint - player.position;
        toShadow.y = 0f;
        Vector3 playerForward = player.forward;
        playerForward.y = 0f;
        float angle = Vector3.Angle(playerForward, toShadow);

        bool looking = angle < hideAngle;

        float targetIntensity = looking ? 0f : baseIntensity;
        currentIntensity = Mathf.MoveTowards(currentIntensity, targetIntensity, fadeSpeed * Time.deltaTime);
        spotLight.intensity = currentIntensity;

        if (currentIntensity <= 0f)
        {
            if (monsterAnimator != null)
                monsterAnimator.enabled = false;
            if (crawlSource != null && crawlSource.isPlaying)
                crawlSource.Stop();
            wasVisible = false;
            return;
        }

        if (monsterAnimator != null)
            monsterAnimator.enabled = true;

        if (!wasVisible && crawlSource != null && crawlClip != null)
        {
            crawlSource.clip = crawlClip;
            crawlSource.Play();
        }
        wasVisible = true;

        transform.position = new Vector3(
            startPos.x,
            startPos.y,
            player.position.z
        );
        // distort
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