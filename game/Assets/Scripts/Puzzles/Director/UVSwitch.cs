using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class UVSwitch : MonoBehaviour
{
    public UVLamp uvLamp;
    public KeypadLight keypadLight;

    [Header("Audio")]
    public AudioClip switchSound;

    private AudioSource audioSource;

    private bool canToggle = true;
    public float cooldown = 1f;

<<<<<<< HEAD
    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f; // 3D sound
        audioSource.volume = 1.5f;
        audioSource.minDistance = 1f;
        audioSource.maxDistance = 8f;
    }

    public void pressSwitch()
    {
        if (keypadLight != null)
        {
            keypadLight.enableKeyPadLight();
        }
=======
    public bool hintTriggered = false;

    public void pressSwitch()
    {

        if (hintTriggered == false)
        {
            hintTriggered = true;
            HintButton.Instance.changeHintState(HintButton.HintState.ObtainLiftKey);
        }

        keypadLight.enableKeyPadLight();
>>>>>>> origin/dev

        if (canToggle)
        {
            // Play switch sound
            if (switchSound != null)
            {
                audioSource.PlayOneShot(switchSound);
            }

            // Flip the switch
            Vector3 scale = transform.localScale;
            scale.y *= -1f;
            transform.localScale = scale;

            // Turn UV light on/off
            if (uvLamp != null)
            {
                uvLamp.ToggleLight();
            }

            canToggle = false;
            Invoke(nameof(ResetToggle), cooldown);
        }
    }

    private void ResetToggle()
    {
        canToggle = true;
    }
}