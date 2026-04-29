using UnityEngine;

public class GirlSpeak1 : MonoBehaviour
{
    public AudioSource nurseryAudio;
    private bool hasPlayed = false;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !hasPlayed)
        {
            hasPlayed = true;
            if (nurseryAudio != null) nurseryAudio.Play();
        }
    }
}