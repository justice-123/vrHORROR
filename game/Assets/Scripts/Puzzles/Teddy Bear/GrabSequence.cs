using System.Collections;
using UnityEngine;

public class TeddyGrabSequence : MonoBehaviour
{
    [Header("Lights")]
    public Light[] roomLights; // drag all room lights in here in the inspector

    [Header("Flickering")]
    public float minFlickerIntensity = 0.05f;
    public float maxFlickerIntensity = 0.3f;
    public float flickerSpeed = 0.05f; // seconds between flicker changes

    [Header("Audio")]
    public AudioSource growlAudioSource; // separate AudioSource with growl clip
    public AudioSource roomAmbienceAudio; // reference to stop the chanting too

    private float[] originalIntensities;

    void Start()
    {
        // Store original light intensities so we know what to return to if needed
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
        // Stop the room ambience (teddy has been taken)
        if (roomAmbienceAudio != null)
            roomAmbienceAudio.Stop();

        // 1 — lights off
        SetLights(false);

        // 2 — 2 seconds of silence in the dark
        yield return new WaitForSeconds(2f);

        // 3 — growl plays
        if (growlAudioSource != null)
            growlAudioSource.Play();

        // wait for growl to finish before lights return
        yield return new WaitForSeconds(growlAudioSource.clip.length);

        // 4 — lights back on, but now flickering dimly
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
        while (true) // flickers forever until scene changes
        {
            foreach (Light l in roomLights)
                l.intensity = Random.Range(minFlickerIntensity, maxFlickerIntensity);

            yield return new WaitForSeconds(flickerSpeed);
        }
    }
}