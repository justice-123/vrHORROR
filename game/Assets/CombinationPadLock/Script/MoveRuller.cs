// Script by Marcelli Michele

using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class MoveRuller : MonoBehaviour
{
    PadLockPassword _lockPassword;
    PadLockEmissionColor _pLockColor;

    [HideInInspector]
    public List<GameObject> _rullers = new List<GameObject>();

    private int _scroolRuller = 0;
    private int _changeRuller = 0;

    [HideInInspector]
    public int[] _numberArray = { 0, 0, 0, 0 };

    private int _numberRuller = 0;
    private bool _isActveEmission = false;

    [Header("Audio")]
    public AudioClip lockMoveSound;

    private AudioSource audioSource;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f; // 3D sound
        audioSource.volume = 1f;
        audioSource.minDistance = 1f;
        audioSource.maxDistance = 8f;

        _lockPassword = FindFirstObjectByType<PadLockPassword>();
        _pLockColor = FindFirstObjectByType<PadLockEmissionColor>();

        _rullers.Add(GameObject.Find("Ruller1"));
        _rullers.Add(GameObject.Find("Ruller2"));
        _rullers.Add(GameObject.Find("Ruller3"));
        _rullers.Add(GameObject.Find("Ruller4"));

        foreach (GameObject r in _rullers)
        {
            r.transform.Rotate(-144, 0, 0, Space.Self);
        }
    }

    public void ScrollRullerUp()
    {
        _isActveEmission = true;

        PlayLockMoveSound();

        _rullers[_changeRuller].transform.Rotate(-36, 0, 0, Space.Self);

        _numberArray[_changeRuller]++;

        if (_numberArray[_changeRuller] > 9)
        {
            _numberArray[_changeRuller] = 0;
        }

        _lockPassword.Password();
    }

    public void PlayerInteractedWithRuller(int index)
    {
        _changeRuller = index;
        ScrollRullerUp();
    }

    private void PlayLockMoveSound()
    {
        if (lockMoveSound != null)
        {
            audioSource.PlayOneShot(lockMoveSound);
        }
        else
        {
            Debug.LogWarning("Lock move sound is missing.", this);
        }
    }
}

