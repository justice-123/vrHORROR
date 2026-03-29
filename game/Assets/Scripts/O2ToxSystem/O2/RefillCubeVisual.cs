using UnityEngine;

public class RefillCubeVisual : MonoBehaviour
{
    public Renderer cubeRenderer;

    [Header("Pulse Settings")]
    public float pulseSpeed = 2f;
    public float minEmission = 0.3f;
    public float maxEmission = 1.5f;

    private Material mat;

    // This will be set by the refill script
    [HideInInspector]
    public bool isRefilling = false;

    void Start()
    {
        if (cubeRenderer != null)
            mat = cubeRenderer.material;
    }

    void Update()
    {
        if (mat == null) return;

        if (isRefilling)
        {
            // steady glow while refilling
            mat.SetColor("_EmissionColor", Color.white * maxEmission);
        }
        else
        {
            // pulsing glow
            float pulse = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
            float emission = Mathf.Lerp(minEmission, maxEmission, pulse);

            mat.SetColor("_EmissionColor", Color.white * emission);
        }
    }
}