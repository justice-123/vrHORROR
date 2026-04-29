using UnityEngine;

public class DoorSwap : MonoBehaviour
{
    public GameObject brokenObject;

    [Header("Audio")]
    public AudioSource brokenSound;

    public void swapToBrokenModel()
    {
        // Activate broken object first
        if (brokenObject != null)
        {
            brokenObject.SetActive(true);
        }

        // Spawn an independent audio object that won't be affected by parent
        if (brokenSound != null && brokenSound.clip != null)
        {
            GameObject audioObj = new GameObject("DoorBreakSound");
            audioObj.transform.position = brokenSound.transform.position;

            AudioSource source = audioObj.AddComponent<AudioSource>();
            source.clip = brokenSound.clip;
            source.volume = brokenSound.volume;
            source.spatialBlend = brokenSound.spatialBlend;
            source.minDistance = brokenSound.minDistance;
            source.maxDistance = brokenSound.maxDistance;
            source.outputAudioMixerGroup = brokenSound.outputAudioMixerGroup;
            source.Play();

            Destroy(audioObj, brokenSound.clip.length + 0.1f);
        }

        gameObject.SetActive(false);
    }
}