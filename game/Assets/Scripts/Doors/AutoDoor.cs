using UnityEngine;
using UnityEngine.UI;

public class AutoDoor : MonoBehaviour
{
    private bool open = false;

    public float smooth = 3.0f;
    public float DoorOpenAngle = 120.0f;

    private Vector3 defaulRot;
    private Vector3 openRot;

    public AudioSource audioSource;
    private bool soundEffectPlayed = false;

    void Start()
    {
        defaulRot = transform.eulerAngles;
        openRot = new Vector3(defaulRot.x, defaulRot.y + DoorOpenAngle, defaulRot.z);
    }

    void Update()
    {
        if (open)
        {
            transform.eulerAngles = Vector3.Slerp(transform.eulerAngles, openRot, Time.deltaTime * smooth);
        
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