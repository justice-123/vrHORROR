using UnityEngine;
using System.Collections;

public class SimpleBGMTrigger : MonoBehaviour
{
    public AudioClip newBGM;
    public float fadeTime = 1.5f;

    private AudioSource bgmSource;
    private Coroutine fadeCoroutine;

    private void Start()
    {
        GameObject bgmObject = GameObject.Find("BGM");

        if (bgmObject != null)
        {
            bgmSource = bgmObject.GetComponent<AudioSource>();
        }

        if (bgmSource == null)
        {
            Debug.LogWarning("Cannot find BGM AudioSource. Please make sure there is a GameObject named 'BGM' with an AudioSource.");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (bgmSource == null)
        {
            Debug.LogWarning("BGM AudioSource is missing.");
            return;
        }

        if (newBGM == null)
        {
            Debug.LogWarning("New BGM is not assigned.");
            return;
        }

        if (bgmSource.clip == newBGM && bgmSource.isPlaying) return;

        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        fadeCoroutine = StartCoroutine(FadeToNewBGM(newBGM));
    }

    private IEnumerator FadeToNewBGM(AudioClip targetClip)
    {
        float originalVolume = bgmSource.volume;

        // Fade out old BGM
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

        // Fade in new BGM
        while (bgmSource.volume < originalVolume)
        {
            bgmSource.volume += originalVolume * Time.deltaTime / fadeTime;
            yield return null;
        }

        bgmSource.volume = originalVolume;
    }
}