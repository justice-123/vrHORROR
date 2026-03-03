using System;
using System.Collections;
using UnityEngine;

public class BreathInput : MonoBehaviour
{
    [Header("Microphone")]
    public string deviceName = null;
    public int sampleRate = 48000;
    public int windowSize = 1024;

    [Header("RMS Zones - Tune These On Quest")]
    [Tooltip("Below this = silence.")]
    public float silenceRms = 0.0001f;

    [Tooltip("Below this = definitely breathing (too quiet for speech).")]
    public float safeBreathRms = 0.002f;

    [Tooltip("Above this = definitely speech (too loud for breathing).")]
    public float speechRms = 0.02f;

    [Tooltip("RMS representing heavy breathing, for 0-1 intensity scaling.")]
    public float heavyBreathRms = 0.005f;

    [Header("Pitch Detection (ambiguous zone only)")]
    public float minPitchHz = 70f;
    public float maxPitchHz = 400f;
    [Range(0f, 1f)]
    public float pitchConfidenceThreshold = 0.4f;
    public int lagStep = 3;

    [Header("Smoothing")]
    public float smoothSpeed = 8f;

    [Header("Breathing Rate")]
    public float bpmWindow = 15f;

    [Header("Outputs - Read Only")]
    [Range(0f, 1f)]
    public float breathIntensity01;
    public bool isBreathing;
    public float breathsPerMinute;
    public float debugRms;
    public string debugZone;
    [Range(0f, 1f)]
    public float debugPitchConfidence;
    public float debugPitchHz;

    private AudioClip micClip;
    private float[] sampleBuffer;
    private float smoothedIntensity;
    private float analysisClock;
    private float analysisInterval = 0.05f;
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
            debugZone = "SILENCE";
            debugPitchConfidence = 0f;
            debugPitchHz = 0f;
            SetBreathState(false, 0f);
            return;
        }

        if (rms < safeBreathRms)
        {
            debugZone = "BREATH (quiet)";
            debugPitchConfidence = 0f;
            debugPitchHz = 0f;
            float intensity = Mathf.Clamp01(Mathf.InverseLerp(silenceRms, heavyBreathRms, rms));
            SetBreathState(true, intensity);
            return;
        }

        if (rms > speechRms)
        {
            debugZone = "SPEECH (loud)";
            debugPitchConfidence = 1f;
            debugPitchHz = 0f;
            SetBreathState(false, 0f);
            return;
        }

        float pitchConf;
        float pitchHz;
        DetectPitch(sampleBuffer, sampleRate, minPitchHz, maxPitchHz,
                    lagStep, out pitchConf, out pitchHz);
        debugPitchConfidence = pitchConf;
        debugPitchHz = pitchHz;

        if (pitchConf < pitchConfidenceThreshold)
        {
            debugZone = "BREATH (pitch check)";
            float intensity = Mathf.Clamp01(Mathf.InverseLerp(silenceRms, heavyBreathRms, rms));
            SetBreathState(true, intensity);
        }
        else
        {
            debugZone = "SPEECH (pitch check)";
            SetBreathState(false, 0f);
        }
    }

    static void DetectPitch(float[] data, int sr, float minHz, float maxHz,
                            int step, out float confidence, out float pitchHz)
    {
        int minLag = Mathf.Max(1, (int)(sr / maxHz));
        int maxLag = Mathf.Min(data.Length / 2, (int)(sr / minHz));
        if (minLag >= maxLag) { confidence = 0f; pitchHz = 0f; return; }
        if (step < 1) step = 1;

        float bestCorr = -1f;
        int bestLag = minLag;
        int n = data.Length;

        for (int lag = minLag; lag <= maxLag; lag += step)
        {
            double sum = 0;
            double e1 = 0;
            double e2 = 0;
            int count = n - lag;
            for (int i = 0; i < count; i++)
            {
                sum += data[i] * data[i + lag];
                e1 += data[i] * data[i];
                e2 += data[i + lag] * data[i + lag];
            }
            double denom = Math.Sqrt(e1 * e2);
            float corr = (denom > 0) ? (float)(sum / denom) : 0f;
            if (corr > bestCorr) { bestCorr = corr; bestLag = lag; }
        }

        confidence = Mathf.Clamp01(bestCorr);
        pitchHz = (bestCorr > 0.2f) ? (float)sr / bestLag : 0f;
    }

    void SetBreathState(bool breathing, float intensity)
    {
        isBreathing = breathing;
        smoothedIntensity = Mathf.Lerp(smoothedIntensity, intensity,
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