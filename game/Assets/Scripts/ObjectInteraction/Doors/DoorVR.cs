using UnityEngine;

public class DoorVR : MonoBehaviour
{
    [Header("Rotation")]
    public float smooth = 2.0f;
    public float doorOpenAngle = 90.0f;

    [Header("State")]
    [SerializeField] private bool isUnlocked;
    [SerializeField] private bool isOpen;

    public AudioSource doorSound;
    public AudioSource lockedSound;

    private Quaternion closedRot;
    private Quaternion openRot;

    void Awake()
    {
        closedRot = transform.rotation;
        openRot = Quaternion.Euler(0f, doorOpenAngle, 0f) * closedRot;
    }

    void Update()
    {

        bool targetOpen = isOpen;

        Quaternion targetRot = targetOpen ? openRot : closedRot;
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * smooth);
    }

    // Called by the lock when the correct key is used.
    public void UnlockAndOpen()
    {
        isUnlocked = true;
        isOpen = true;
    }

    public void LockAndClose()
    {
        isUnlocked = false;
        isOpen = false;
    }

    public bool IsUnlocked() => isUnlocked;
}
