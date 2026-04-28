using System.Collections;
using UnityEngine;

public class TeddyGrabSequence : MonoBehaviour
{
    [Header("Lights")]
    public Light[] roomLights;
    public float flickerDelay = 2f; // how many seconds after grab before flicker starts

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
        if (roomAmbienceAudio != null)
            roomAmbienceAudio.Stop();

        // lights off
        SetLights(false);

        // start flicker on timer regardless of growl
        StartCoroutine(DelayedFlicker(flickerDelay));

        // growl plays after 2 seconds of silence
        yield return new WaitForSeconds(2f);
        if (growlAudioSource != null)
            growlAudioSource.Play();
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