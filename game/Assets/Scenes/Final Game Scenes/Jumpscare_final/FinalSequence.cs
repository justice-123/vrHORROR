using System.Collections;
using UnityEngine;
using UnityEngine.LowLevel;

public class FinalSequence : MonoBehaviour
{
    public SpiderJumpscare spider;
    public Transform playerCamera;

    public void StartSequence()
    {
        StartCoroutine(Sequence());
    }

    IEnumerator Sequence()
    {
        // 1. Lock player
        FindObjectOfType<PlayerLock>().LockPlayer();

        // 2. Small "freedom" delay (IMPORTANT)
        yield return new WaitForSeconds(1.5f);

        // 3. Play subtle audio cue (behind player)
        // (You can use a 3D sound placed behind them)

        yield return new WaitForSeconds(1f);

        // 4. Spawn spider BEHIND player
        Vector3 spawnPos = playerCamera.position
                         - playerCamera.forward * 5f;

        spider.transform.position = spawnPos;

        // Face the player
        spider.transform.LookAt(playerCamera);

        // 5. Trigger jumpscare
        spider.TriggerJumpscare();
    }
}