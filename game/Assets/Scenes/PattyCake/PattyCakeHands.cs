using UnityEngine;
using UnityEngine.XR;

public class PattyCakeHands : MonoBehaviour
{

    //public AudioClip clapSound;
    //private AudioSource audioSource;
    [Header("References")]
    public PattyCakeGameManager gameManager;

    [Header("Hand Setup")]
    public bool isLeftHand = true;

    [Header("Haptics")]
    [Range(0f, 1f)] public float hapticAmplitude = 0.5f;
    public float hapticDuration = 0.1f;

    private float clapCooldown = 0.3f;
    private float lastClapTime = 0f;



    void OnTriggerEnter(Collider other)
    {
        Debug.Log("Collision");

        if (Time.time > lastClapTime + clapCooldown)
        {
            if (other.CompareTag("TargetBox"))
            {
                // Ask the manager if we hit the correct box
                bool correctHit = gameManager.TryHit(other.gameObject);

                if (correctHit)
                {
                    TriggerHaptic();
                    lastClapTime = Time.time;
                    Debug.Log("Buzz");

                }
            }
        }




    }

    void TriggerHaptic()
    {
        if (isLeftHand)
        {
            InputDevices.GetDeviceAtXRNode(XRNode.LeftHand).SendHapticImpulse(0u, hapticAmplitude, hapticDuration);
        }
        else
        {
            InputDevices.GetDeviceAtXRNode(XRNode.RightHand).SendHapticImpulse(0u, hapticAmplitude, hapticDuration);
        }
    }
}



