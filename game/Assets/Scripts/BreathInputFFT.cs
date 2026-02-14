using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class BreathInputFFT : MonoBehaviour
{
    [Header("Mic")]
    public string deviceName = null;
    public int clipLengthSec = 1;

    [Header("FFT")]
    public int fftSize = 1024;
    public FFTWindow window = FFTWindow.BlackmanHarris;

    [Header("Bands (Hz)")]
    public float lowMinHz = 100f;
    public float lowMaxHz = 800f;
    public float midMinHz = 800f;
    public float midMaxHz = 3000f;

    [Header("Calibration (tune in play mode)")]
    public float baselineBreathiness = 0.8f;
    public float loudBreathiness = 2.0f;

    [Header("Smoothing")]
    public float smoothSpeed = 2f;

    [Header("Rate Detection")]
    [Range(0f, 1f)] public float eventThreshold01 = 0.55f;
    public float minBreathInterval = 0.8f;
    public float maxBreathInterval = 10f;

    [Header("Outputs")]
    [Range(0f, 1f)] public float breathNoise01;
    public float breathinessRaw;
    public float breathsPerMinute;

    AudioClip micClip;
    AudioSource src;
    float[] spectrum;
    float smoothed01;

    bool lastAbove;
    float lastEventTime = -999f;

    int clipSampleRate = 48000; // will be overwritten by micClip.frequency

    IEnumerator Start()
    {
        src = GetComponent<AudioSource>();

        // Force “2D always” + predictable behaviour
        src.spatialBlend = 0f;
        src.loop = true;
        src.playOnAwake = false;

        // IMPORTANT:
        // Some Quest/Android paths can produce ~0 spectrum if AudioSource.mute = true.
        // So instead of muting, set very low volume.
        src.mute = false;
        src.volume = 0.001f;

        spectrum = new float[fftSize];

#if UNITY_ANDROID
        if (!Application.HasUserAuthorization(UserAuthorization.Microphone))
        {
            Application.RequestUserAuthorization(UserAuthorization.Microphone);
            while (!Application.HasUserAuthorization(UserAuthorization.Microphone))
                yield return null;
        }
#endif

        if (Microphone.devices.Length == 0)
        {
            Debug.LogError("No microphone devices found.");
            yield break;
        }

        if (string.IsNullOrEmpty(deviceName))
            deviceName = Microphone.devices[0];

        // Let Android choose the best sample rate (0 is the key)
        micClip = Microphone.Start(deviceName, true, clipLengthSec, 0);

        while (Microphone.GetPosition(deviceName) <= 0)
            yield return null;

        clipSampleRate = micClip.frequency;
        Debug.Log($"Mic device: {deviceName}  clip freq: {micClip.frequency}  channels: {micClip.channels}");

        src.clip = micClip;

        // Restart cleanly (helps on device)
        src.Stop();
        src.Play();
    }

    void OnDisable()
    {
        if (!string.IsNullOrEmpty(deviceName) && Microphone.IsRecording(deviceName))
            Microphone.End(deviceName);
    }

    void Update()
    {
        if (!src || !src.isPlaying || micClip == null) return;

        // --- Sanity: is spectrum non-zero at all?
        src.GetSpectrumData(spectrum, 0, window);

        // Use the REAL sample rate for bin mapping
        float nyquist = clipSampleRate * 0.5f;

        int HzToBin(float hz)
        {
            int bin = Mathf.RoundToInt(hz / nyquist * (fftSize - 1));
            return Mathf.Clamp(bin, 0, fftSize - 1);
        }

        int lowMin = HzToBin(lowMinHz);
        int lowMax = HzToBin(lowMaxHz);
        int midMin = HzToBin(midMinHz);
        int midMax = HzToBin(midMaxHz);

        if (lowMax <= lowMin || midMax <= midMin) return;

        float eLow = 0f;
        for (int i = lowMin; i <= lowMax; i++)
        {
            float m = spectrum[i];
            eLow += m * m;
        }

        float eMid = 0f;
        for (int i = midMin; i <= midMax; i++)
        {
            float m = spectrum[i];
            eMid += m * m;
        }

        const float eps = 1e-8f;
        breathinessRaw = eLow / (eMid + eps);

        float raw01 = Mathf.InverseLerp(baselineBreathiness, loudBreathiness, breathinessRaw);
        raw01 = Mathf.Clamp01(raw01);

        smoothed01 = Mathf.Lerp(smoothed01, raw01, 1f - Mathf.Exp(-smoothSpeed * Time.deltaTime));
        breathNoise01 = smoothed01;

        bool above = breathNoise01 > eventThreshold01;

        if (above && !lastAbove)
        {
            float now = Time.time;
            float interval = now - lastEventTime;

            if (interval >= minBreathInterval && interval <= maxBreathInterval)
                breathsPerMinute = 60f / interval;

            lastEventTime = now;
        }

        lastAbove = above;

        Debug.Log($"Raw: {breathinessRaw}  01: {breathNoise01}  BPM: {breathsPerMinute}");
    }
}
