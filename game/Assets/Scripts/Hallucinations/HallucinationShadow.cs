using UnityEngine;

public class HallucinationShadow : MonoBehaviour
{
    public ToxicityDevice toxicity;
    public Light shadowLight;
    public float activateAbove = 40f;

    public float rotateSpeed = 15f;
    public float bobAmount = 0.5f;
    public float bobSpeed = 1f;

    Vector3 startPos;
    float baseIntensity;
    bool active;

    void Start()
    {
        if (toxicity == null)
            toxicity = FindAnyObjectByType<ToxicityDevice>();

        startPos = shadowLight.transform.localPosition;
        baseIntensity = shadowLight.intensity;
        shadowLight.intensity = 0f;
    }

    void Update()
    {
        active = toxicity.toxicityLevel >= activateAbove;

        if (!active)
        {
            shadowLight.intensity = 0f;
            return;
        }

        float t = (toxicity.toxicityLevel - activateAbove)
                / (100f - activateAbove);

        shadowLight.intensity = Mathf.Lerp(0f, baseIntensity, t);

        shadowLight.transform.Rotate(Vector3.forward, rotateSpeed * t * Time.deltaTime);

        float bob = Mathf.Sin(Time.time * bobSpeed) * bobAmount * t;
        shadowLight.transform.localPosition = startPos + Vector3.up * bob;
    }
}