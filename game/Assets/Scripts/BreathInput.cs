using System.Collections;
using UnityEngine;

public class BreathInput : MonoBehaviour
{
    [Header("Mic")]
    public string deviceName = null;
    public int sampleRate = 48000;
    public int clipLengthSec = 1;
    public int windowSamples = 1024;

    [Header("Calibration (tune in play mode)")]
    public float baselineRms = 0.0002f;  // to TUNE -> quiet / normal
    public float loudRms = 0.0020f;  // loud breathing / talking

    [Header("Smoothing")]
    public float smoothSpeed = 10f;

    [Header("Output")]
    [Range(0f, 1f)] public float breathNoise01;

    private AudioClip micClip;
    private float smoothed;

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
        micClip = Microphone.Start(deviceName, true, clipLengthSec, sampleRate);
        yield return new WaitForSeconds(0.2f);
    }

    void OnDisable()
    {
        if (Microphone.IsRecording(deviceName))
            Microphone.End(deviceName);
    }

    void Update()
    {
        if (micClip == null) return;

        int micPos = Microphone.GetPosition(deviceName);
        if (micPos < windowSamples) return;

        float rms = ComputeRms(micClip, micPos, windowSamples);

        float raw01 = Mathf.InverseLerp(baselineRms, loudRms, rms);
        raw01 = Mathf.Clamp01(raw01);

        smoothed = Mathf.Lerp(smoothed, raw01, 1f - Mathf.Exp(-smoothSpeed * Time.deltaTime));
        breathNoise01 = smoothed;
    }

    float ComputeRms(AudioClip clip, int micPos, int count)
    {
        float[] data = new float[count];
        int start = micPos - count;
        if (start < 0) start += clip.samples;

        clip.GetData(data, start);

        double sum = 0;
        for (int i = 0; i < data.Length; i++)
            sum += data[i] * data[i];

        return Mathf.Sqrt((float)(sum / data.Length));
    }
}
