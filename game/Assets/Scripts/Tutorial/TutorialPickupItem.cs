using UnityEngine;


public class TutorialPickupItem : MonoBehaviour
{
    [Header("Spawn Settings")]
    public GameObject itemPrefab;  
    public Vector3 spawnPosition = new Vector3(0f, 1.2f, 0.5f);
    public Vector3 spawnRotation = Vector3.zero;

    [Header("Particles")]
    public ParticleSystem sparkleParticles; 

    // Reference to the spawned item, used by the tutorial to listen for grabs
    [HideInInspector] public GameObject spawnedItem;

    void Start()
    {
        if (itemPrefab != null)
        {
            spawnedItem = Instantiate(itemPrefab, spawnPosition, Quaternion.Euler(spawnRotation));
            Debug.Log($"[TutorialPickupItem] Spawned {itemPrefab.name} at {spawnPosition}");

            // Move particles onto the spawned item so they follow it
            if (sparkleParticles != null)
            {
                sparkleParticles.transform.SetParent(spawnedItem.transform);
                sparkleParticles.transform.localPosition = Vector3.zero;
                sparkleParticles.transform.localScale = Vector3.one * 0.2f;
            }
        }
        else
        {
            Debug.LogError("[TutorialPickupItem] No prefab assigned!");
        }
    }

    // Call this after the item is picked up to stop the sparkle effect
    public void StopParticles()
    {
        if (sparkleParticles != null)
        {
            sparkleParticles.Stop();
            sparkleParticles.gameObject.SetActive(false);
        }
    }

    // Call this to remove the item after the tutorial finishes
    public void DestroyItem()
    {
        if (spawnedItem != null)
            Destroy(spawnedItem);
    }
}