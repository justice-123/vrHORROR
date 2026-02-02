using UnityEngine;
using TMPro;

public class QuestMicMeter : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text micText;
    public UnityEngine.UI.Image meterFill;

    [Header("Mic Settings")]
    public int sampleRate = 48000;
    public int clipLengthSec = 1;          // short looping buffer
    public float meterSmoothing = 12f;     // higher = smoother

    private AudioClip micClip;
    private string micDevice;
    private float smoothed01 = 0f;

    // audio analysis buffer
    private const int AnalysisSamples = 1024;
    private float[] samples = new float[AnalysisSamples];

    void Start()
    {
        StartMic();
    }

    void Update()
    {
        if (micClip == null) return;

        float rms = GetRmsLevel(micClip, micDevice);
        float db = RmsToDb(rms);

        // Map RMS -> 0..1 meter (tweak these for breathing sensitivity)
        // breathing often lives low, so we amplify the mapping a bit
        float target01 = Mathf.Clamp01(rms * 25f);

        smoothed01 = Mathf.Lerp(smoothed01, target01, Time.deltaTime * meterSmoothing);

        if (meterFill != null) meterFill.fillAmount = smoothed01;

        if (micText != null)
        {
            micText.text =
                $"Mic: {(string.IsNullOrEmpty(micDevice) ? "None" : micDevice)}\n" +
                $"RMS: {rms:F5}\n" +
                $"dB:  {db:F1}\n" +
                $"Meter: {smoothed01:F2}\n\n" +
                $"Tip: Try mouth breathing close to headset, then nasal, then normal distance.";
        }
    }

    void StartMic()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        // Request runtime mic permission on Android
        if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Microphone))
        {
            UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.Microphone);
        }
#endif
        if (Microphone.devices.Length == 0)
        {
            Debug.LogError("No microphone devices found.");
            return;
        }

        micDevice = Microphone.devices[0];
        Debug.Log("Using mic: " + micDevice);

        // Start recording into a looping AudioClip
        micClip = Microphone.Start(micDevice, true, clipLengthSec, sampleRate);

        // Wait until mic starts (avoid reading before ready)
        int safety = 0;
        while (Microphone.GetPosition(micDevice) <= 0 && safety < 1000000) safety++;

        Debug.Log("Mic started.");
    }

    float GetRmsLevel(AudioClip clip, string device)
    {
        int micPos = Microphone.GetPosition(device);
        if (micPos < AnalysisSamples) return 0f;

        // Read the most recent chunk of audio
        int start = micPos - AnalysisSamples;
        clip.GetData(samples, start);

        double sum = 0;
        for (int i = 0; i < samples.Length; i++)
        {
            float s = samples[i];
            sum += s * s;
        }

        return Mathf.Sqrt((float)(sum / samples.Length));
    }

    float RmsToDb(float rms)
    {
        if (rms <= 1e-7f) return -80f;
        return 20f * Mathf.Log10(rms);
    }
}
