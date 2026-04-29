using UnityEngine;

public class HallucinationAudio : MonoBehaviour
{
    public ToxicityDevice toxicity;
    public AudioSource audioSource;
    public AudioClip[] clips;
    public float activateAbove = 40f;
    public float minDelay = 2f;
    public float maxDelay = 15f;

    float timer;

    void Start()
    {
        if (toxicity == null)
            toxicity = FindAnyObjectByType<ToxicityDevice>();

        timer = Random.Range(minDelay, maxDelay);
    }

    void Update()
    {
        if (toxicity.toxicityLevel < activateAbove) return;
        if (audioSource.isPlaying) return;

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            audioSource.clip = clips[Random.Range(0, clips.Length)];
            audioSource.Play();
            timer = Random.Range(minDelay, maxDelay);
        }
    }
}