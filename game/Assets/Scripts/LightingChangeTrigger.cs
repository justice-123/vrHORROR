using System.Collections;
using UnityEngine;

public class LightingChangeTrigger : MonoBehaviour
{
    [SerializeField] GameObject[] lightObjectsToDisable;
    [SerializeField] CandleSpawner candleSpawner;

    void OnTriggerEnter(Collider other)
    {
        foreach (GameObject obj in lightObjectsToDisable)
        {
            Light[] lights = obj.GetComponentsInChildren<Light>();
            foreach (Light light in lights)
                light.enabled = false;
        }

        candleSpawner.ActivateCandles();
        Debug.Log("activating candles");
    }
}