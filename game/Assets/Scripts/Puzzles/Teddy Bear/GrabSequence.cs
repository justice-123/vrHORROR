using System.Collections;
using UnityEngine;

public class TeddyGrabSequence : MonoBehaviour
{
    [Header("Lights")]
    public Light[] roomLights;
    public float flickerDelay = 2f;

    [Header("Flickering")]
    public float minFlickerIntensity = 0.05f;
    public float maxFlickerIntensity = 0.3f;
    public float flickerSpeed = 0.05f;

    [Header("Audio")]
    public AudioSource growlAudioSource;
    public AudioSource roomAmbienceAudio;

    private float[] originalIntensities;

    void Start()
    {
        originalIntensities = new float[roomLights.Length];
        for (int i = 0; i < roomLights.Length; i++)
            originalIntensities[i] = roomLights[i].intensity;
    }

    public void StartSequence()
    {
        StartCoroutine(GrabSequence());
    }

    IEnumerator GrabSequence()
    {
        StartCoroutine(FadeOutAmbience(0.8f));

        SetLights(false);
        StartCoroutine(DelayedFlicker(flickerDelay));

        yield return new WaitForSeconds(1.8f);
        if (growlAudioSource != null)
            growlAudioSource.Play();
    }

    IEnumerator FadeOutAmbience(float fadeDuration)
    {
        float startVolume = roomAmbienceAudio.volume;
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            roomAmbienceAudio.volume = Mathf.Lerp(startVolume, 0f, elapsed / fadeDuration);
            yield return null;
        }
        roomAmbienceAudio.volume = 0f;
        roomAmbienceAudio.Stop();
    }

    IEnumerator DelayedFlicker(float delay)
    {
        yield return new WaitForSeconds(delay);
        SetLights(true);
        StartCoroutine(Flicker());
    }

    void SetLights(bool on)
    {
        foreach (Light l in roomLights)
            l.enabled = on;
    }

    IEnumerator Flicker()
    {
        while (true)
        {
            foreach (Light l in roomLights)
                l.intensity = Random.Range(minFlickerIntensity, maxFlickerIntensity);
            yield return new WaitForSeconds(flickerSpeed);
        }
    }
}