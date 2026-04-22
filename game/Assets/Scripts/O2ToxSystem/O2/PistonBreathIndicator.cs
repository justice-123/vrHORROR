using UnityEngine;

public class PistonBreathIndicator : MonoBehaviour
{
    public Transform piston;
    public BreathInputML breathInput;

    [Header("Movement")]
    public float pistonDownY = 0f;
    public float pistonUpY = 0.05f;
    public float speed = 8f;

    private float targetY;

    void Update()
    {
        if (breathInput == null || piston == null) return;

        targetY = breathInput.isBreathing ? pistonUpY : pistonDownY;

        Vector3 pos = piston.localPosition;
        pos.y = Mathf.Lerp(pos.y, targetY, Time.deltaTime * speed);
        piston.localPosition = pos;
    }
}