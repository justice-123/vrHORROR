using UnityEngine;

public class GirlBear : MonoBehaviour
{
    public GameObject axePrefab;
    public Animator animator;
    public string animTriggerName = "ReceiveBear";
    public Transform playerTransform;      

    [Header("Float Settings")]
    public float floatHeight = 1.5f;
    public float floatDuration = 3f;
    public float swayAmount = 0.3f;
    public float swaySpeed = 2f;
    public float rotateSpeed = 60f;
    public float waitAfterAnim = 1.375f;
    public float fadeStartPercent = 0.7f;

    [Header("Axe Throw Settings")]
    public float axeForwardForce = 5f;     
    public float axeUpForce = 3f;         
    public float axeSpinSpeed = 720f;     
    public float axeSpawnBehindDistance = 1.5f; 

    private bool activated = false;
    private bool floating = false;
    private float waitTimer = 0f;
    private float floatTimer = 0f;
    private Vector3 startPos;
    private bool axeSpawned = false;
    private Renderer[] renderers;

    void Start()
    {
        renderers = GetComponentsInChildren<Renderer>();
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                playerTransform = player.transform;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!activated && other.CompareTag("Player")
            && InventoryManager.Instance.getActiveItemID() == "teddy")
        {
            activated = true;
            startPos = transform.position;

            if (animator != null)
                animator.SetTrigger(animTriggerName);

            InventoryManager.Instance.DeleteCurrentItem();
            waitTimer = waitAfterAnim;
        }
    }

    void Update()
    {
        if (!activated) return;

        if (!floating)
        {
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0f)
                floating = true;
            return;
        }

        floatTimer += Time.deltaTime;
        float progress = Mathf.Clamp01(floatTimer / floatDuration);
        float easedProgress = progress * progress;

        float newY = startPos.y + easedProgress * floatHeight;
        float sway = Mathf.Sin(floatTimer * swaySpeed) * swayAmount;
        transform.position = new Vector3(startPos.x + sway, newY, startPos.z);
        transform.Rotate(0f, rotateSpeed * Time.deltaTime, 0f);

        if (progress > fadeStartPercent)
        {
            float fadeProgress = (progress - fadeStartPercent) / (1f - fadeStartPercent);
            SetAlpha(1f - fadeProgress);
        }

        if (!axeSpawned && progress > 0.4f)
        {
            axeSpawned = true;
            SpawnAxeFromBehindPlayer();
            MySceneManager.Instance.LoadNewScene("Final-monster-game-scene");
        }

        if (progress >= 1f)
            Destroy(gameObject);
    }

    void SpawnAxeFromBehindPlayer()
    {
        if (playerTransform == null) return;

        Vector3 behindPlayer = playerTransform.position
            - playerTransform.forward * axeSpawnBehindDistance
            + Vector3.up * 2f;

        Vector3 target = startPos + Vector3.up * 0.05f;

        GameObject axe = Instantiate(axePrefab, behindPlayer, Quaternion.identity);

        Rigidbody rb = axe.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        Collider col = axe.GetComponent<Collider>();
        if (col != null)
            col.enabled = false;


        AxeSpin spin = axe.AddComponent<AxeSpin>();
        spin.spinSpeed = axeSpinSpeed;
        spin.startPos = behindPlayer;
        spin.targetPos = target;
        spin.flyDuration = 0.8f;   
        spin.arcHeight = 2.5f;     
    }

    void SetAlpha(float alpha)
    {
        foreach (Renderer r in renderers)
        {
            foreach (Material mat in r.materials)
            {
                Color c = mat.color;
                c.a = alpha;
                mat.color = c;
                if (alpha < 1f)
                {
                    mat.SetFloat("_Surface", 1);
                    mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    mat.renderQueue = 3000;
                }
            }
        }
    }
}