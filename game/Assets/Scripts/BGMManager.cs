using UnityEngine;
using System.Collections;

public class BGMManager : MonoBehaviour
{
    public static BGMManager Instance;

    public AudioSource bgmSource;
    public float fadeTime = 1.5f;

    private Coroutine fadeCoroutine;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void PlayBGM(AudioClip newClip)
    {
        if (newClip == null)
        {
            Debug.LogWarning("No BGM clip assigned.");
            return;
        }

        if (bgmSource.clip == newClip) return;

        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        fadeCoroutine = StartCoroutine(FadeToNewBGM(newClip));
    }

    private IEnumerator FadeToNewBGM(AudioClip newClip)
    {
        float originalVolume = bgmSource.volume;

        while (bgmSource.volume > 0)
        {
            bgmSource.volume -= originalVolume * Time.deltaTime / fadeTime;
            yield return null;
        }

        bgmSource.Stop();
        bgmSource.clip = newClip;
        bgmSource.loop = true;
        bgmSource.Play();

        while (bgmSource.volume < originalVolume)
        {
            bgmSource.volume += originalVolume * Time.deltaTime / fadeTime;
            yield return null;
        }

        bgmSource.volume = originalVolume;
    }
}
