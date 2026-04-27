using UnityEngine;
using UnityEngine.UI;

public class KeypadDoor : MonoBehaviour
{
    private bool open = false;

    public float smooth = 3.0f;
    public float DoorOpenAngle = 120.0f;

    private Vector3 defaulRot;
    private Vector3 openRot;

    public AudioClip dooropen;
    private AudioSource audioSource;


    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f; // 3D sound
        audioSource.volume = 1f;
    }


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

    public void correctCombination()
    {
        open = true;

        audioSource.PlayOneShot(dooropen);
    }
}