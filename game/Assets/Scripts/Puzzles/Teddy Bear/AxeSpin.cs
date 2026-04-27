using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AxeSpin : MonoBehaviour
{
    public float spinSpeed = 720f;
    public Vector3 startPos;
    public Vector3 targetPos;
    public float flyDuration = 0.8f;
    public float arcHeight = 2.5f;

    [Header("Audio")]
    public AudioClip axeDropSound;

    private AudioSource audioSource;

    private float timer = 0f;
    private bool arrived = false;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;
        audioSource.volume = 1f;
        audioSource.minDistance = 1f;
        audioSource.maxDistance = 10f;
    }

    void Update()
    {
        if (arrived) return;

        timer += Time.deltaTime;
        float progress = Mathf.Clamp01(timer / flyDuration);

        float easedProgress = 1f - Mathf.Pow(1f - progress, 2f);

        Vector3 currentPos = Vector3.Lerp(startPos, targetPos, easedProgress);
        float arc = arcHeight * 4f * progress * (1f - progress);
        currentPos.y += arc;

        transform.position = currentPos;
        transform.Rotate(spinSpeed * Time.deltaTime, 0f, 0f);

        if (progress >= 1f)
        {
            arrived = true;

            PlayAxeDropSound();

            Collider col = GetComponent<Collider>();
            if (col != null)
                col.enabled = true;

            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
            }
        }
    }

    private void PlayAxeDropSound()
    {
        if (axeDropSound != null)
        {
            audioSource.PlayOneShot(axeDropSound);
        }
    }
}