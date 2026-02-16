using UnityEngine;
using Bhaptics.SDK2;

public class PaperGrabHaptic : MonoBehaviour
{
    [Header("Haptic Settings")]
    [SerializeField] private string hapticEventName = "hearthump";
    [SerializeField] private int intensity = 1;
    [SerializeField] private int duration = 200;
    [SerializeField] private bool continuousHeartbeat = false;
    [SerializeField] private float beatsPerMinute = 100f;

    private OVRGrabbable grabbable;
    private bool isGrabbed = false;
    private float beatInterval;

    void Start()
    {
        grabbable = GetComponent<OVRGrabbable>();
        beatInterval = 60f / beatsPerMinute;
    }

    void Update()
    {
        // Check if paper is currently grabbed
        if (grabbable.isGrabbed && !isGrabbed)
        {
            // Just grabbed
            OnPaperGrabbed();
            isGrabbed = true;
        }
        else if (!grabbable.isGrabbed && isGrabbed)
        {
            // Just released
            OnPaperReleased();
            isGrabbed = false;
        }
    }

    void OnPaperGrabbed()
    {
        // Play haptic when grabbed
        BhapticsLibrary.Play(hapticEventName, intensity, duration);

        // Optional: Start continuous heartbeat while holding
        if (continuousHeartbeat)
        {
            InvokeRepeating("PlayHeartbeat", beatInterval, beatInterval);
        }
    }

    void OnPaperReleased()
    {
        // Stop continuous heartbeat when released
        if (continuousHeartbeat)
        {
            CancelInvoke("PlayHeartbeat");
        }
    }

    void PlayHeartbeat()
    {
        BhapticsLibrary.Play(hapticEventName, intensity, duration);
    }
}