using UnityEngine;
using System.Collections;

public class DoorLock : MonoBehaviour
{
    public TutorialDoor tutorialDoor;
    public bool unlocked;
    public AudioSource lockAudio;

    [Header("BGM Switch")]
    public AudioClip newBGM;
    public float fadeTime = 1.5f;

    void Start()
    {
        unlocked = false;
    }

    public void OnTriggerEnter(Collider other)
    {
        if (!unlocked && InventoryManager.Instance.getActiveItemID() == "doorkey")
        {
            unlocked = true;
            tutorialDoor.unlockLock();

            if (lockAudio != null)
                lockAudio.Play();

            InventoryManager.Instance.DeleteCurrentItem();
            HintButton.Instance.changeHintState(HintButton.HintState.ExploreFirstArea);

            // Switch BGM when door unlocks
            if (newBGM != null)
                StartCoroutine(SwitchBGM());
        }
    }

    private IEnumerator SwitchBGM()
    {
        GameObject bgmObject = GameObject.Find("BGM");
        if (bgmObject == null) yield break;

        AudioSource bgmSource = bgmObject.GetComponent<AudioSource>();
        if (bgmSource == null) yield break;

        float originalVolume = bgmSource.volume;

        // Fade out
        while (bgmSource.volume > 0.01f)
        {
            bgmSource.volume -= originalVolume * Time.deltaTime / fadeTime;
            yield return null;
        }

        bgmSource.volume = 0f;
        bgmSource.Stop();
        bgmSource.clip = newBGM;
        bgmSource.loop = true;
        bgmSource.Play();

        // Fade in
        while (bgmSource.volume < originalVolume)
        {
            bgmSource.volume += originalVolume * Time.deltaTime / fadeTime;
            yield return null;
        }

        bgmSource.volume = originalVolume;
    }
}