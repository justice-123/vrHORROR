using System;
using System.Collections;
using UnityEngine;


public class BreathInput : MonoBehaviour
{
    // Mic Settings
    [Header("Microphone")]
    [Tooltip("Null = default mic")]
    public string deviceName = null;
    public int sampleRate = 48000;

    // Analysis
    [Header("Analysis")]
    [Tooltip("How many samples to analyse per window")]
    public int windowSize = 2048;

    [Tooltip("How often to analyse (seconds)")]
    public float analysisInterval = 0.15f;

    // Pitch detection range (human voice is roughly 80-400 Hz)
    [Header("Pitch Detection")]
    [Tooltip("Lowest pitch to search for (Hz) - lower = deeper voice")]
    public float minPitchHz = 70f;

    [Tooltip("Highest pitch to search for (Hz)")]
    public float maxPitchHz = 400f;

    [Tooltip("Autocorrelation peak below this = no pitch detected = breath")]
    [Range(0f, 1f)]
    public float pitchConfidenceThreshold = 0.4f;

    // Thresholds
    [Header("Thresholds - Tune In Play Mode")]
    [Tooltip("RMS below this = silence")]
    public float silenceRms = 0.003f;

    [Tooltip("RMS value representing heavy breathing")]
    public float heavyBreathRms = 0.04f;

    // Smoothing
    [Header("Smoothing")]
    public float smoothSpeed = 8f;

    // Breathing Rate
    [Header("Breathing Rate")]
    public float bpmWindow = 15f;

    // Outputs
    [Header("Outputs - Read Only")]
    [Range(0f, 1f)]
    public float breathIntensity01;

    public bool isBreathing;

    public float breathsPerMinute;

    [Tooltip("How confident the detector is that pitch is present (0=noise, 1=clear pitch)")]
    [Range(0f, 1f)]
    public float debugPitchConfidence;

    [Tooltip("Detected pitch frequency in Hz (0 if no pitch)")]
    public float debugPitchHz;

    public float debugRms;

    // Private
    private AudioClip micClip;
    private float[] sampleBuffer;
    private float smoothedIntensity;
    private float analysisClock;

    private bool wasBreathing;
    private float lastBreathOnsetTime;
    private float[] breathIntervals;
    private int intervalIndex;
    private int intervalCount;
    private const int MaxIntervals = 30;

    IEnumerator Start()
    {
#if UNITY_ANDROID
        if (!Application.HasUserAuthorization(UserAuthorization.Microphone))
        {
            Application.RequestUserAuthorization(UserAuthorization.Microphone);
            while (!Application.HasUserAuthorization(UserAuthorization.Microphone))
                yield return null;
        }
#endif
        sampleBuffer = new float[windowSize];
        breathIntervals = new float[MaxIntervals];

        micClip = Microphone.Start(deviceName, true, 1, sampleRate);
        while (Microphone.GetPosition(deviceName) <= 0)
            yield return null;
    }

    void OnDisable()
    {
        if (Microphone.IsRecording(deviceName))
            Microphone.End(deviceName);
    }

    void Update()
    {
        if (micClip == null) return;

        analysisClock += Time.deltaTime;
        if (analysisClock < analysisInterval) return;
        analysisClock = 0f;

        int micPos = Microphone.GetPosition(deviceName);
        if (micPos < windowSize) return;

        int start = micPos - windowSize;
        if (start < 0) start += micClip.samples;
        micClip.GetData(sampleBuffer, start);


        float rms = ComputeRms(sampleBuffer);
        debugRms = rms;

        if (rms < silenceRms)
        {
            debugPitchConfidence = 0f;
            debugPitchHz = 0f;
            SetBreathState(false, 0f);
            return;
        }


        float pitchConfidence;
        float pitchHz;
        DetectPitch(sampleBuffer, sampleRate, minPitchHz, maxPitchHz,
                    out pitchConfidence, out pitchHz);

        debugPitchConfidence = pitchConfidence;
        debugPitchHz = pitchHz;


        bool breathDetected = pitchConfidence < pitchConfidenceThreshold;

        float intensity = breathDetected
            ? Mathf.Clamp01(Mathf.InverseLerp(silenceRms, heavyBreathRms, rms))
            : 0f;

        SetBreathState(breathDetected, intensity);
    }


    static void DetectPitch(float[] data, int sr, float minHz, float maxHz,
                            out float confidence, out float pitchHz)
    {

        int minLag = Mathf.Max(1, (int)(sr / maxHz));
        int maxLag = Mathf.Min(data.Length / 2, (int)(sr / minHz));

        if (minLag >= maxLag)
        {
            confidence = 0f;
            pitchHz = 0f;
            return;
        }

       
        double energy = 0;
        for (int i = 0; i < data.Length; i++)
            energy += data[i] * data[i];

        if (energy <= 0)
        {
            confidence = 0f;
            pitchHz = 0f;
            return;
        }

      
        float bestCorr = -1f;
        int bestLag = minLag;
        int n = data.Length;

        for (int lag = minLag; lag <= maxLag; lag++)
        {
            double sum = 0;
            double energy1 = 0;
            double energy2 = 0;
            int count = n - lag;

            for (int i = 0; i < count; i++)
            {
                sum += data[i] * data[i + lag];
                energy1 += data[i] * data[i];
                energy2 += data[i + lag] * data[i + lag];
            }

           
            double denom = Math.Sqrt(energy1 * energy2);
            float corr = (denom > 0) ? (float)(sum / denom) : 0f;

            if (corr > bestCorr)
            {
                bestCorr = corr;
                bestLag = lag;
            }
        }

        confidence = Mathf.Clamp01(bestCorr);
        pitchHz = (bestCorr > 0.2f) ? (float)sr / bestLag : 0f;
    }

    // Breath state and BPM

    void SetBreathState(bool breathing, float intensity)
    {
        isBreathing = breathing;

        smoothedIntensity = Mathf.Lerp(
            smoothedIntensity, intensity,
            1f - Mathf.Exp(-smoothSpeed * Time.deltaTime));
        breathIntensity01 = smoothedIntensity;

        if (breathing && !wasBreathing)
        {
            float now = Time.time;
            if (lastBreathOnsetTime > 0f)
            {
                float interval = now - lastBreathOnsetTime;
                if (interval > 1f && interval < 12f)
                {
                    breathIntervals[intervalIndex] = interval;
                    intervalIndex = (intervalIndex + 1) % MaxIntervals;
                    if (intervalCount < MaxIntervals) intervalCount++;
                }
            }
            lastBreathOnsetTime = now;
        }
        wasBreathing = breathing;

        breathsPerMinute = ComputeBpm();
    }

    float ComputeBpm()
    {
        if (intervalCount == 0) return 0f;

        float sum = 0f;
        int count = 0;

        for (int i = 0; i < intervalCount; i++)
        {
            int idx = (intervalIndex - 1 - i + MaxIntervals) % MaxIntervals;
            sum += breathIntervals[idx];
            count++;
            if (sum > bpmWindow) break;
        }

        if (count == 0) return 0f;
        return 60f / (sum / count);
    }

    static float ComputeRms(float[] data)
    {
        double sum = 0;
        for (int i = 0; i < data.Length; i++)
            sum += data[i] * data[i];
        return Mathf.Sqrt((float)(sum / data.Length));
    }
}