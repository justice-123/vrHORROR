using UnityEngine;
using UnityEngine.UI;

public class AutoDoor : MonoBehaviour
{
    private bool open = false;

    public float smooth = 3.0f;
    public float DoorOpenAngle = 120.0f;

    private Quaternion defaultRot;
    private Quaternion openRot;

    public AudioSource audioSource;
    private bool soundEffectPlayed = false;

    void Start()
    {
        defaultRot = transform.localRotation;
        openRot = defaultRot * Quaternion.Euler(0, DoorOpenAngle, 0);
    }

    void Update()
    {
        if (open)
        {
            transform.localRotation = Quaternion.Slerp(transform.localRotation, openRot, Time.deltaTime * smooth);
        
        }
    }

    private void OnTriggerEnter(Collider coll)
    {
        if (coll.CompareTag("Player") && !soundEffectPlayed)
        {
            open = true;
            if (audioSource != null) audioSource.Play();
        }

        soundEffectPlayed = true;
    }
}