using UnityEngine;

public class StorageTankRefill : MonoBehaviour
{
    private OxygenTank oxygenTank;
    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    private void OnTriggerEnter(Collider other)
    {
        oxygenTank = other.GetComponentInParent<OxygenTank>();

        if (oxygenTank != null)
        {
            oxygenTank.isRefilling = true;

            if (audioSource != null && !audioSource.isPlaying)
            {
                audioSource.Play();
            }
        }
    }

    void Update()
    {
        if (oxygenTank != null && oxygenTank.isRefilling)
        {
            if (oxygenTank.oxygenLevel >= 100f)
            {
                oxygenTank.isRefilling = false;

                if (audioSource != null && audioSource.isPlaying)
                {
                    audioSource.Stop();
                }
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        OxygenTank tank = other.GetComponentInParent<OxygenTank>();

        if (tank != null && tank == oxygenTank)
        {
            tank.isRefilling = false;

            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Stop();
            }

            oxygenTank = null;
        }
    }
}