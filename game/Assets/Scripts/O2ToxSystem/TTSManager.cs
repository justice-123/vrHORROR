using UnityEngine;

public class TTSManager : MonoBehaviour
{
    public static TTSManager Instance { get; private set; }

    private AndroidJavaObject tts;
    private bool ready = false;

    void Awake()
    {
        Instance = this;

        if (Application.platform == RuntimePlatform.Android)
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

            tts = new AndroidJavaObject("android.speech.tts.TextToSpeech", activity, new TTSInitListener(this));
        }
    }

    public void SetReady() { ready = true; }

    public void Speak(string message)
    {
        if (!ready || tts == null) return;
        tts.Call<int>("speak", message, 0, null, null); // 0 = QUEUE_FLUSH
    }

    void OnDestroy()
    {
        if (tts != null)
        {
            tts.Call("stop");
            tts.Call("shutdown");
        }
    }
}

public class TTSInitListener : AndroidJavaProxy
{
    private TTSManager manager;

    public TTSInitListener(TTSManager manager)
        : base("android.speech.tts.TextToSpeech$OnInitListener")
    {
        this.manager = manager;
    }

    public void onInit(int status)
    {
        if (status == 0) // SUCCESS
            manager.SetReady();
    }
}