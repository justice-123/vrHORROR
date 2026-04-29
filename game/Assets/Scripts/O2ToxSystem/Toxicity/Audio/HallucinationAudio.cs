using UnityEngine;
using System.Collections;

public class HallucinationAudio : MonoBehaviour
{
    public ToxicityDevice toxicity;
    public AudioSource audioSource;
    public AudioClip[] clips;
    public float activateAbove = 40f;
    public float minDelay = 5f;
    public float maxDelay = 15f;
    public float fadeDuration = 1f;

    float timer;
    float baseVolume;

    void Start()
    {
        if (toxicity == null)
            toxicity = FindAnyObjectByType<ToxicityDevice>();
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        baseVolume = audioSource.volume;
        timer = Random.Range(minDelay, maxDelay);
    }

    void Update()
    {
        if (toxicity == null || toxicity.toxicityLevel < activateAbove) return;
        if (audioSource.isPlaying) return;

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            AudioClip clip = clips[Random.Range(0, clips.Length)];
            audioSource.clip = clip;

            if (clip.name.ToLower().Contains("whisper"))
            {
                StartCoroutine(PlayWithFade(clip));
            }
            else
            {
                audioSource.volume = baseVolume;
                audioSource.Play();
            }

            timer = Random.Range(minDelay, maxDelay);
        }
    }

    IEnumerator PlayWithFade(AudioClip clip)
    {
        float clipLength = clip.length;
        float fadeIn = Mathf.Min(fadeDuration, clipLength * 0.4f);
        float fadeOut = Mathf.Min(fadeDuration, clipLength * 0.4f);
        float fadeOutStart = clipLength - fadeOut;

        // start silent
        audioSource.volume = 0f;
        audioSource.Play();

        // fade in
        float elapsed = 0f;
        while (elapsed < fadeIn)
        {
            elapsed += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(0f, baseVolume, elapsed / fadeIn);
            yield return null;
        }
        audioSource.volume = baseVolume;

        // wait until fade out point
        float waitTime = fadeOutStart - fadeIn;
        if (waitTime > 0f)
            yield return new WaitForSeconds(waitTime);

        // fade out
        elapsed = 0f;
        while (elapsed < fadeOut)
        {
            elapsed += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(baseVolume, 0f, elapsed / fadeOut);
            yield return null;
        }
        audioSource.volume = 0f;
    }
}