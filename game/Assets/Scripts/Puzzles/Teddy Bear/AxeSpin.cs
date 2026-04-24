using UnityEngine;

public class AxeSpin : MonoBehaviour
{
    public float spinSpeed = 720f;
    public Vector3 startPos;
    public Vector3 targetPos;
    public float flyDuration = 0.8f;
    public float arcHeight = 2.5f;

    private float timer = 0f;
    private bool arrived = false;

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
            transform.position = targetPos;

            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
    }
}