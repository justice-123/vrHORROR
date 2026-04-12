using UnityEngine;

public class HallucinationFlicker : MonoBehaviour
{
    public ToxicityDevice toxicity;
    public Light[] lights;
    public float activateAbove = 40f;

    float[] baseIntensities;
    float timer;

    void Start()
    {
        if (toxicity == null)
            toxicity = FindAnyObjectByType<ToxicityDevice>();

        baseIntensities = new float[lights.Length];
        for (int i = 0; i < lights.Length; i++)
            baseIntensities[i] = lights[i].intensity;
    }

    void Update()
    {
        if (toxicity.toxicityLevel < activateAbove)
        {
            for (int i = 0; i < lights.Length; i++)
                lights[i].intensity = baseIntensities[i];
            return;
        }

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            for (int i = 0; i < lights.Length; i++)
                StartCoroutine(Flicker(i));

            timer = Random.Range(0.5f, 2f);
        }
    }

    System.Collections.IEnumerator Flicker(int index)
    {
        yield return new WaitForSeconds(Random.Range(0f, 1f));

        int flicks = Random.Range(2, 5);
        for (int i = 0; i < flicks; i++)
        {
            lights[index].intensity = Random.Range(0f, baseIntensities[index] * 0.3f);
            yield return new WaitForSeconds(Random.Range(0.03f, 0.08f));
        }
        lights[index].intensity = baseIntensities[index];
    }
}