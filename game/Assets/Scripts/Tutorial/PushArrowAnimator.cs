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
    public float moveDistance = 0.3f;  // forward travel distance (meters)
    public float moveDuration = 0.6f;  // duration of single move
    public float pauseDuration = 0.3f;  // pause between each loop
    public Vector3 offset = new Vector3(0f, 0f, 0.1f); // offset relative to controller

    // Added: reverse controller for backward arrows
    public Transform reverseLeftController;
    public Transform reverseRightController;

    GameObject leftArrow;
    GameObject rightArrow;
    GameObject reverseLeftArrow;
    GameObject reverseRightArrow;
    bool playing = false;

    // ── Public API ────────────────────────────────

    public void StartArrows()
    {
        if (playing) return;
        playing = true;

        // Forward arrows
        if (leftController != null)
        {
            leftArrow = CreateArrow("LeftArrow");
            StartCoroutine(AnimateArrow(leftArrow, leftController, false));
        }

        if (rightController != null)
        {
            rightArrow = CreateArrow("RightArrow");
            StartCoroutine(AnimateArrow(rightArrow, rightController, false));
        }

        // Backward arrows
        if (reverseLeftController != null)
        {
            reverseLeftArrow = CreateArrow("ReverseLeftArrow");
            StartCoroutine(AnimateArrow(reverseLeftArrow, reverseLeftController, true));
        }

        if (reverseRightController != null)
        {
            reverseRightArrow = CreateArrow("ReverseRightArrow");
            StartCoroutine(AnimateArrow(reverseRightArrow, reverseRightController, true));
        }
    }

    public void StopArrows()
    {
        playing = false;
        if (leftArrow) Destroy(leftArrow);
        if (rightArrow) Destroy(rightArrow);
        if (reverseLeftArrow) Destroy(reverseLeftArrow);
        if (reverseRightArrow) Destroy(reverseRightArrow);

        // Clear all references
        reverseLeftController = null;
        reverseRightController = null;
    }

    // ── Arrow Creation ────────────────────────────

    GameObject CreateArrow(string name)
    {
        var go = new GameObject(name);

        // Use URP unlit shader so arrows always display correctly
        var mat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
        if (mat.shader.name == "Hidden/InternalErrorShader")
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        }
        mat.SetFloat("_Surface", 1); // transparent surface type
        mat.color = arrowColor;

        // Arrow shaft (LineRenderer)
        var lr = go.AddComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.startWidth = arrowWidth;
        lr.endWidth = arrowWidth * 0.3f;
        lr.useWorldSpace = true;
        lr.material = mat;
        lr.startColor = arrowColor;
        lr.endColor = arrowColor;

        // Arrow head (triangle, second LineRenderer)
        var head = new GameObject("ArrowHead");
        head.transform.SetParent(go.transform);
        var headLr = head.AddComponent<LineRenderer>();
        headLr.positionCount = 4;
        headLr.startWidth = arrowWidth * 2.5f;
        headLr.endWidth = 0f;
        headLr.useWorldSpace = true;
        headLr.material = mat;
        headLr.startColor = arrowColor;
        headLr.endColor = arrowColor;

        return go;
    }

    // ── Animation Coroutine ───────────────────────
    // reverse=true makes the arrow move backward

    IEnumerator AnimateArrow(GameObject arrow, Transform controller, bool reverse)
    {
        var lr = arrow.GetComponent<LineRenderer>();
        var headLr = arrow.transform.GetChild(0).GetComponent<LineRenderer>();

        // Direction multiplier: 1 for forward, -1 for backward
        float dir = reverse ? -1f : 1f;

        while (playing)
        {
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / moveDuration;
                float ease = EaseInOutCubic(Mathf.Clamp01(t));

                // Base position = controller position + offset
                Vector3 basePos = controller.TransformPoint(offset);

                // Slide arrow along controller's forward axis (reversed if needed)
                Vector3 forward = controller.forward * dir;
                Vector3 slidePos = basePos + forward * (ease * moveDistance);

                // Shaft: extend 0.12m along the direction from slidePos
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
                lr.startColor = c;
                lr.endColor = c;
                headLr.startColor = c;
                headLr.endColor = c;

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