using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class Button : MonoBehaviour
{
    public DoorMovement doorMovement;

    private AudioSource audioSource;
    private bool doorTriggered = false;

    private float startY = 0.915f;
    private float pressedY = 0.945f;
    private float moveDuration = 0.2f;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f; // 3D sound
        audioSource.volume = 1.5f;
    }

    public void buttonInteraction()
    {
        if (!doorTriggered)
        {
            doorTriggered = true;

            PlayButtonSound();

            StartCoroutine(buttonPress());

            if (doorMovement != null)
            {
                doorMovement.closeDoors();
            }
        }
    }

    private void PlayButtonSound()
    {
        if (audioSource.clip != null)
        {
            audioSource.PlayOneShot(audioSource.clip);
        }
        else
        {
            Debug.LogWarning("Button audio clip is missing.", this);
        }
    }

    public IEnumerator buttonPress()
    {
        yield return moveButton(pressedY);

        yield return new WaitForSeconds(0.05f);

        yield return moveButton(startY);
    }

    public IEnumerator moveButton(float targetY)
    {
        Vector3 start = transform.localPosition;
        Vector3 end = new Vector3(start.x, targetY, start.z);
        float elapsed = 0;

        while (elapsed < moveDuration)
        {
            transform.localPosition = Vector3.Lerp(start, end, elapsed / moveDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localPosition = end;
    }
}
