using UnityEngine;
using System.Collections;

public class BGMToggleTrigger : MonoBehaviour
{
    public AudioClip bgm1;
    public AudioClip bgm2;
    public float fadeTime = 1.5f;

    private AudioSource bgmSource;
    private Coroutine fadeCoroutine;
    private bool playingFirst = true;

    private void Start()
    {
        GameObject bgmObject = GameObject.Find("BGM");
        if (bgmObject != null)
        {
            bgmSource = bgmObject.GetComponent<AudioSource>();
        }
        if (bgmSource == null)
        {
            Debug.LogWarning("Cannot find BGM AudioSource.");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (bgmSource == null) return;

        AudioClip targetClip = playingFirst ? bgm2 : bgm1;
        playingFirst = !playingFirst;

        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }
        fadeCoroutine = StartCoroutine(FadeToNewBGM(targetClip));
    }

    private IEnumerator FadeToNewBGM(AudioClip targetClip)
    {
        float originalVolume = bgmSource.volume;

        while (bgmSource.volume > 0.01f)
        {
            bgmSource.volume -= originalVolume * Time.deltaTime / fadeTime;
            yield return null;
        }

        bgmSource.volume = 0f;
        bgmSource.Stop();
        bgmSource.clip = targetClip;
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