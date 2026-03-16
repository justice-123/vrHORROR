using System.Collections;
using UnityEngine;

public class CandleSpawner : MonoBehaviour
{
    [SerializeField] private GameObject candlePrefab;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private float spawnInterval = 0.3f;
    [SerializeField] private float lightFadeInDuration = 1.5f;

    public void ActivateCandles()
    {
        StartCoroutine(SpawnSequence());
    }

    private IEnumerator SpawnSequence()
    {
        foreach (Transform point in spawnPoints)
        {
            GameObject candle = Instantiate(candlePrefab, point.position, point.rotation);
            Light candleLight = candle.GetComponentInChildren<Light>();
            if (candleLight != null)
                StartCoroutine(FadeInLight(candleLight));
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private IEnumerator FadeInLight(Light light)
    {
        float targetIntensity = light.intensity;
        light.intensity = 0f;
        light.enabled = true;
        float elapsed = 0f;

        while (elapsed < lightFadeInDuration)
        {
            elapsed += Time.deltaTime;
            light.intensity = Mathf.Lerp(0f, targetIntensity, elapsed / lightFadeInDuration);
            yield return null;
        }

        light.intensity = targetIntensity;
    }
}