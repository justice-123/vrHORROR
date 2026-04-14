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

        if (toxicity == null)
            Debug.LogError("HallucinationFlicker: Could not find ToxicityDevice!");
        else
            Debug.Log("HallucinationFlicker: Found ToxicityDevice, level = " + toxicity.toxicityLevel);

        baseIntensities = new float[lights.Length];
        for (int i = 0; i < lights.Length; i++)
            baseIntensities[i] = lights[i].intensity;

        Debug.Log("HallucinationFlicker: " + lights.Length + " lights assigned");
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
        yield return new WaitForSeconds(Random.Range(0f, 0.15f));

        // quick flicker before going dark
        int flicks = Random.Range(2, 4);
        for (int i = 0; i < flicks; i++)
        {
            lights[index].intensity = 0f;
            yield return new WaitForSeconds(Random.Range(0.03f, 0.1f));
            lights[index].intensity = baseIntensities[index];
            yield return new WaitForSeconds(Random.Range(0.03f, 0.1f));
        }

        // stay off for a while
        lights[index].intensity = 0f;
        yield return new WaitForSeconds(Random.Range(5f, 10f));

        // quick flicker back on
        for (int i = 0; i < 3; i++)
        {
            lights[index].intensity = baseIntensities[index];
            yield return new WaitForSeconds(Random.Range(0.03f, 0.08f));
            lights[index].intensity = 0f;
            yield return new WaitForSeconds(Random.Range(0.03f, 0.08f));
        }

        lights[index].intensity = baseIntensities[index];
    }
}