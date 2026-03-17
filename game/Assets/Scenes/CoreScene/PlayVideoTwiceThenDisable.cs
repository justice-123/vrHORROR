using UnityEngine;
using UnityEngine.Video;

[RequireComponent(typeof(VideoPlayer))]
public class PlayVideoTwiceThenDisable : MonoBehaviour
{
    private VideoPlayer videoPlayer;
    private int playCount = 0;
    public int targetPlayTimes = 2;

    void Start()
    {
        videoPlayer = GetComponent<VideoPlayer>();
        videoPlayer.isLooping = false;
        videoPlayer.loopPointReached += OnVideoFinished;
        videoPlayer.Play();
    }

    void OnVideoFinished(VideoPlayer vp)
    {
        playCount++;

        if (playCount < targetPlayTimes)
        {
            vp.Play();
        }
        else
        {
            DisableVideo();
            MySceneManager.Instance.LoadNewScene("PattyCake-1");
        }
    }

    void DisableVideo()
    {
        gameObject.SetActive(false);
    }
}