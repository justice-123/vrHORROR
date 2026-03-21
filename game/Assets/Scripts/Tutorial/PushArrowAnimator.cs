using System.Collections;
using UnityEngine;

public class PushArrowAnimator : MonoBehaviour
{
    [Header("Controllers")]
    public Transform leftController;
    public Transform rightController;

    [Header("Arrow Appearance")]
    public Color arrowColor = Color.white;
    public float arrowWidth = 0.01f;

    [Header("Animation Settings")]
    public float moveDistance = 0.3f;   // forward travel distance (meters)
    public float moveDuration = 0.6f;   // duration of single move
    public float pauseDuration = 0.3f;  // pause between each loop
    public Vector3 offset = new Vector3(0f, 0f, 0.1f); // offset relative to controller

    GameObject leftArrow;
    GameObject rightArrow;
    bool playing = false;

    // ── Public API ────────────────────────────────

    public void StartArrows()
    {
        if (playing) return;
        playing = true;
        leftArrow  = CreateArrow("LeftArrow");
        rightArrow = CreateArrow("RightArrow");
        StartCoroutine(AnimateArrow(leftArrow,  leftController));
        StartCoroutine(AnimateArrow(rightArrow, rightController));
    }

    public void StopArrows()
    {
        playing = false;
        if (leftArrow)  Destroy(leftArrow);
        if (rightArrow) Destroy(rightArrow);
    }

    // ── Arrow Creation ────────────────────────────

    GameObject CreateArrow(string name)
    {
        var go = new GameObject(name);

        // Arrow shaft (LineRenderer)
        var lr = go.AddComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.startWidth = arrowWidth;
        lr.endWidth   = arrowWidth * 0.3f;
        lr.useWorldSpace = true;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = arrowColor;
        lr.endColor   = arrowColor;

        // Arrow head (triangle, second LineRenderer)
        var head = new GameObject("ArrowHead");
        head.transform.SetParent(go.transform);
        var headLr = head.AddComponent<LineRenderer>();
        headLr.positionCount = 4;
        headLr.startWidth = arrowWidth * 2.5f;
        headLr.endWidth   = 0f;
        headLr.useWorldSpace = true;
        headLr.material = new Material(Shader.Find("Sprites/Default"));
        headLr.startColor = arrowColor;
        headLr.endColor   = arrowColor;

        return go;
    }

    // ── Animation Coroutine ───────────────────────

    IEnumerator AnimateArrow(GameObject arrow, Transform controller)
    {
        var lr     = arrow.GetComponent<LineRenderer>();
        var headLr = arrow.transform.GetChild(0).GetComponent<LineRenderer>();

        while (playing)
        {
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / moveDuration;
                float ease = EaseInOutCubic(Mathf.Clamp01(t));

                // Base position = controller position + offset
                Vector3 basePos = controller.TransformPoint(offset);

                // Slide arrow forward along controller's forward axis
                Vector3 forward  = controller.forward;
                Vector3 slidePos = basePos + forward * (ease * moveDistance);

                // Shaft: extend 0.12m forward from slidePos
                Vector3 tailPos = slidePos;
                Vector3 headPos = slidePos + forward * 0.12f;

                lr.SetPosition(0, tailPos);
                lr.SetPosition(1, headPos);

                // Head: triangle shape
                Vector3 right = controller.right;
                float hw = arrowWidth * 3f;
                headLr.SetPosition(0, headPos - right * hw);
                headLr.SetPosition(1, headPos + forward * 0.04f);
                headLr.SetPosition(2, headPos + right * hw);
                headLr.SetPosition(3, headPos - right * hw);

                // Fade in/out using sine curve
                float alpha = Mathf.Sin(Mathf.PI * Mathf.Clamp01(t));
                var c = arrowColor;
                c.a = alpha;
                lr.startColor     = c;
                lr.endColor       = c;
                headLr.startColor = c;
                headLr.endColor   = c;

                yield return null;
            }

            yield return new WaitForSeconds(pauseDuration);
        }
    }

    float EaseInOutCubic(float t)
    {
        return t < 0.5f
            ? 4f * t * t * t
            : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;
    }
}