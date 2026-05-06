using System.Collections;
using UnityEngine;

public class ProjectorTrigger : MonoBehaviour
{
    public bool projectorTriggered;
    public Light projectorLight;
    public AudioSource startup;
    public AudioSource loop;


    
    void Start()
    {
        projectorTriggered = false;
        projectorLight.intensity = 0f;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player" && !projectorTriggered)
        {
            projectorTriggered = true;
            StartCoroutine(TriggerProjector());
            StartCoroutine(ProjectorAudio());
        }
    }

    IEnumerator TriggerProjector()
    {

        while (true)
        {
            projectorLight.intensity = Random.Range(0.2f, 2f);
            yield return new WaitForSeconds(Random.Range(0.01f, 0.1f));

            if (Random.value > 0.95f)
            {
                projectorLight.intensity = 0f;
                yield return new WaitForSeconds(0.05f);
            }
        }
    }

    IEnumerator ProjectorAudio()
    {
        startup.Play();

        yield return new WaitForSeconds(startup.clip.length);

        loop.Play();
    }
}
