using System;
using System.Collections;
using UnityEngine;

public class BreathSpikeDetector : MonoBehaviour
{
    [Header("Mic")]
    public string deviceName = null;
    public int sampleRate = 48000;
    public int clipLengthSec = 1;
    public int windowSamples = 1024;

    [Header("Spike Detection")]
    [Tooltip("Minimum RMS required before we consider spike detection (filters tiny noise).")]
    public float minRms = 0.00025f;

    [Tooltip("How big the frame-to-frame RMS jump must be to count as a spike.")]
    public float spikeDeltaThreshold = 0.00035f;

    [Tooltip("Seconds between spike triggers (prevents spamming).")]
    public float cooldown = 0.6f;

    [Header("Debug (read-only)")]
    public float rms;
    public float delta;

    public event Action OnSpike;

    private AudioClip micClip;
    private float prevRms;
    private float lastSpikeTime = -999f;

    IEnumerator Start()
    {
        if (Microphone.devices.Length == 0)
        {
            Debug.LogWarning("No microphone devices found.");
            yield break;
        }

        if (string.IsNullOrEmpty(deviceName))
            deviceName = Microphone.devices[0];

        micClip = Microphone.Start(deviceName, true, clipLengthSec, sampleRate);

        // Wait until the mic actually starts
        while (Microphone.GetPosition(deviceName) <= 0)
            yield return null;
    }

    void Update()
    {
        if (micClip == null) return;

        rms = ComputeRmsLatest(windowSamples);
        delta = rms - prevRms;
        prevRms = rms;

        bool canTrigger = (Time.time - lastSpikeTime) >= cooldown;
        bool loudEnough = rms >= minRms;
        bool sharpEnough = delta >= spikeDeltaThreshold;

        if (canTrigger && loudEnough && sharpEnough)
        {
            lastSpikeTime = Time.time;
            OnSpike?.Invoke();
        }
    }

    float ComputeRmsLatest(int n)
    {
        n = Mathf.Clamp(n, 256, 4096);

        int micPos = Microphone.GetPosition(deviceName);
        if (micPos < n) return 0f;

        float[] samples = new float[n];
        micClip.GetData(samples, micPos - n);

        double sum = 0.0;
        for (int i = 0; i < samples.Length; i++)
        {
            float s = samples[i];
            sum += s * s;
        }

        return Mathf.Sqrt((float)(sum / samples.Length));
    }
}
