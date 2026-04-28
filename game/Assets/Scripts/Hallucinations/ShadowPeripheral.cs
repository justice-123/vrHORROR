using UnityEngine;

public class ShadowPeripheral : MonoBehaviour
{
    public ToxicityDevice toxicity;
    public Light spotLight;
    public Transform monsterModel;
    public Animator monsterAnimator;
    public float activateAbove = 40f;
    public float hideAngle = 30f;
    public float fadeSpeed = 15f;
    public float respawnDelay = 6f;
    public float moveThreshold = 0.01f;

    [Header("Distortion")]
    public float distortSpeed = 2f;
    public float distortAmount = 0.3f;

    [Header("Audio")]
    public AudioSource crawlSource;
    public AudioClip crawlClip;

    Transform player;
    Vector3 startPos;
    float baseIntensity;
    float currentIntensity;
    float respawnTimer;
    Vector3 lastPlayerPos;
    bool playerMoving;

    void Start()
    {
        if (toxicity == null)
            toxicity = FindAnyObjectByType<ToxicityDevice>();

        player = Camera.main.transform;
        startPos = transform.position;
        baseIntensity = spotLight.intensity;
        currentIntensity = 0f;
        spotLight.intensity = 0f;
        lastPlayerPos = player != null ? player.position : Vector3.zero;

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
            return;
        }

        if (player == null) return;

        // check if player is moving
        Vector3 currentPlayerPos = player.position;
        currentPlayerPos.y = 0f;
        Vector3 lastFlat = lastPlayerPos;
        lastFlat.y = 0f;
        float moved = Vector3.Distance(currentPlayerPos, lastFlat);
        playerMoving = moved > moveThreshold;
        lastPlayerPos = player.position;

        // waiting to respawn
        if (respawnTimer > 0f)
        {
            respawnTimer -= Time.deltaTime;
            currentIntensity = Mathf.MoveTowards(currentIntensity, 0f, fadeSpeed * Time.deltaTime);
            spotLight.intensity = currentIntensity;
            if (currentIntensity <= 0f)
            {
                if (monsterAnimator != null)
                    monsterAnimator.enabled = false;
                if (crawlSource != null && crawlSource.isPlaying)
                    crawlSource.Stop();
            }
            return;
        }

        // raycast from light to wall
        int layerMask = ~LayerMask.GetMask("ShadowOnly", "Ignore Raycast");
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

        // if player looked at it, start respawn cooldown
        if (looking && currentIntensity > 0f)
        {
            respawnTimer = respawnDelay;
        }

        // fade in or out
        float targetIntensity = looking ? 0f : baseIntensity;
        currentIntensity = Mathf.MoveTowards(currentIntensity, targetIntensity, fadeSpeed * Time.deltaTime);
        spotLight.intensity = currentIntensity;

        if (currentIntensity <= 0f)
        {
            if (monsterAnimator != null)
                monsterAnimator.enabled = false;
            if (crawlSource != null && crawlSource.isPlaying)
                crawlSource.Stop();
            return;
        }

        // animation and sound only when player is moving
        if (playerMoving)
        {
            if (monsterAnimator != null)
                monsterAnimator.enabled = true;

            if (crawlSource != null && crawlClip != null && !crawlSource.isPlaying)
            {
                crawlSource.clip = crawlClip;
                crawlSource.loop = true;
                crawlSource.Play();
            }
        }
        else
        {
            if (monsterAnimator != null)
                monsterAnimator.enabled = false;
            if (crawlSource != null && crawlSource.isPlaying)
                crawlSource.Stop();
        }

        // follow player along corridor
        transform.position = new Vector3(
            startPos.x,
            startPos.y,
            player.position.z
        );
    }

    void LateUpdate()
    {
        if (toxicity == null || toxicity.toxicityLevel < activateAbove)
        {
            spotLight.transform.localScale = Vector3.one;
            return;
        }

        if (respawnTimer > 0f) return;
        if (player == null) return;

        // face same direction as player
        Vector3 playerForwardFlat = player.forward;
        playerForwardFlat.y = 0f;
        if (playerForwardFlat.sqrMagnitude > 0.001f)
            monsterModel.rotation = Quaternion.LookRotation(playerForwardFlat);

        // distort
        float t = (toxicity.toxicityLevel - activateAbove)
                / (100f - activateAbove);
        float time = Time.time * distortSpeed;
        float scaleX = 1f + Mathf.Sin(time) * distortAmount * t;
        float scaleY = 1f + Mathf.Sin(time * 1.3f) * distortAmount * t;

        spotLight.transform.localScale = new Vector3(scaleX, scaleY, 1f);
    }
}