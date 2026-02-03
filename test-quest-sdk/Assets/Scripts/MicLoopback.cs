using UnityEngine;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

public class MicLoopback : MonoBehaviour
{
    [Header("Audio")]
    public AudioSource audioSource;
    public int sampleRate = 48000;
    // max length 10 seconds
    public int recordLengthSeconds = 10;

    //  mic gain to make breathing louder.
    public float micGain = 6f;

    //volume multiplier 
    public float playbackVolume = 1.5f;

    public bool normaliseOnPlayback = true;

    public bool logDevices = true;
    public bool logLoudness = false;

    private AudioClip micClip;
    private bool isRecording;
    private string deviceName = null; 

    void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = false;
    }

    void Start()
    {
        RequestMicPermission();

        if (logDevices)
        {
            Debug.Log("Microphone found: " + Microphone.devices.Length);
            for (int i = 0; i < Microphone.devices.Length; i++)
                Debug.Log($"Mic[{i}]: {Microphone.devices[i]}");
        }
    }

    void Update()
    {
        // A = start/stop recording
        if (OVRInput.GetDown(OVRInput.Button.One))
        {
            if (!isRecording) StartRecording();
            else StopRecording();
        }

        // B = playback
        if (OVRInput.GetDown(OVRInput.Button.Two))
        {
            PlayLastRecording();
        }

        if (logLoudness && micClip != null && isRecording)
        {
            Debug.Log("Live loudness: " + GetMicLoudness().ToString("F4"));
        }
    }

    void RequestMicPermission()
    {
#if UNITY_ANDROID
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            Permission.RequestUserPermission(Permission.Microphone);
        }
#endif
    }

    public void StartRecording()
    {
        if (isRecording) return;

        if (Microphone.devices.Length == 0)
        {
            Debug.LogError("No microphone found.");
            return;
        }

        audioSource.Stop();

        micClip = Microphone.Start(deviceName, false, recordLengthSeconds, sampleRate);
        isRecording = true;

        Debug.Log("Recording started.");
    }

    public void StopRecording()
    {
        if (!isRecording) return;

        // Wait until mic has actually started
        int safety = 0;
        while (Microphone.GetPosition(deviceName) <= 0 && safety < 1000000)
            safety++;

        Microphone.End(deviceName);
        isRecording = false;

        Debug.Log("Recording stopped.");
    }

    public void PlayLastRecording()
    {
        if (micClip == null)
        {
            Debug.LogWarning("No recording.");
            return;
        }

        AudioClip clipToPlay = micClip;

        if (normaliseOnPlayback)
        {
            clipToPlay = CreateAmplifiedClip(micClip);
        }

        audioSource.Stop();
        audioSource.clip = clipToPlay;
        audioSource.volume = playbackVolume;
        audioSource.time = 0f;
        audioSource.Play();

        Debug.Log("Playback started.");
    }


    AudioClip CreateAmplifiedClip(AudioClip original)
    {
        float[] samples = new float[original.samples];
        original.GetData(samples, 0);

        float max = 0f;
        for (int i = 0; i < samples.Length; i++)
            max = Mathf.Max(max, Mathf.Abs(samples[i]));

        float gain = (max > 0.0001f) ? micGain / max : micGain;

        for (int i = 0; i < samples.Length; i++)
            samples[i] = Mathf.Clamp(samples[i] * gain, -1f, 1f);

        AudioClip newClip = AudioClip.Create(
            original.name + "_amplified",
            original.samples,
            original.channels,
            original.frequency,
            false
        );

        newClip.SetData(samples, 0);
        return newClip;
    }

    float GetMicLoudness()
    {
        int micPos = Microphone.GetPosition(deviceName);
        if (micPos < 256) return 0f;

        float[] samples = new float[256];
        micClip.GetData(samples, micPos - 256);

        float sum = 0f;
        for (int i = 0; i < samples.Length; i++)
            sum += samples[i] * samples[i];

        return Mathf.Sqrt(sum / samples.Length);
    }
}
