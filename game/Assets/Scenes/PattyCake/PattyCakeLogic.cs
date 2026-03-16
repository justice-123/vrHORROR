using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.InputSystem;

public class PattyCakeLogic : MonoBehaviour
{

    public AudioClip clapSound;
    private AudioSource audioSource;

    public string targetTag = "TargetLeftBox";

    private float clapCooldown = 0.3f;
    private float lastClapTime = 0f;


    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f; // 3d sound

    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log("Collision");

        if (other.CompareTag(targetTag) && Time.time > lastClapTime + clapCooldown)
        {
            Debug.Log("Clap");

            audioSource.PlayOneShot(clapSound);
            lastClapTime = Time.time;
        }
        
    }
}
