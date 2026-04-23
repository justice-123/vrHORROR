using System.Collections;
using UnityEngine;

public class JumpscareController : MonoBehaviour
{
    [Header("Spider Setup")]
    public GameObject spiderObject;
    public Animator spiderAnimator;

    [Header("Audio")]
    public AudioSource giggleSource;
    public AudioSource shriekSource;

    [Header("Player")]
    public Transform playerCamera; // Drag your VR camera here

    [Header("Settings")]
    public float safetyWaitTime = 10f;   // How long player feels safe
    public float spiderSpawnDistance = 15f; // How far behind player spider spawns

    private bool sceneStarted = false;

    // Call this from your escape trigger
    public void StartFinalScene()
    {
        if (!sceneStarted)
        {
            sceneStarted = true;
            StartCoroutine(FinalScareSequence());
        }
    }

    IEnumerator FinalScareSequence()
    {
        // === PHASE 1: Player feels safe ===
        spiderObject.SetActive(false);
        yield return new WaitForSeconds(safetyWaitTime);

        // === PHASE 2: Play giggle behind player ===
        PlaceSpiderBehindPlayer();
        spiderObject.SetActive(true);
        spiderAnimator.Play("Idle");
        giggleSource.Play();

        // Silence after giggle
        yield return new WaitForSeconds(1.5f);

        // === PHASE 3: Wait until player looks at spider ===
        yield return new WaitUntil(() => PlayerIsLookingAtSpider());

        // === PHASE 4: ATTACK ===
        shriekSource.Play();
        spiderAnimator.SetTrigger("Attack");
        StartCoroutine(ChargeAtPlayer());
    }

    void PlaceSpiderBehindPlayer()
    {
        // Get the direction behind the player
        Vector3 behindPlayer = -playerCamera.forward;
        behindPlayer.y = 0; // Keep it on the ground level
        behindPlayer.Normalize();

        // Place spider behind player at set distance
        Vector3 spawnPos = playerCamera.position + behindPlayer * spiderSpawnDistance;
        spawnPos.y = 0; // Ground level - adjust this if your floor isn't at y=0
        spiderObject.transform.position = spawnPos;

        // Make spider face the player
        spiderObject.transform.LookAt(playerCamera.position);
    }

    bool PlayerIsLookingAtSpider()
    {
        // Get direction from camera to spider
        Vector3 directionToSpider = spiderObject.transform.position - playerCamera.position;
        directionToSpider.Normalize();

        // Check the angle between camera forward and direction to spider
        float angle = Vector3.Angle(playerCamera.forward, directionToSpider);

        // If spider is within 40 degrees of where player is looking, trigger attack
        return angle < 40f;
    }

    IEnumerator ChargeAtPlayer()
    {
        float chargeSpeed = 8f;
        float elapsed = 0f;

        while (elapsed < 3f)
        {
            elapsed += Time.deltaTime;

            // Move spider toward player head
            Vector3 target = playerCamera.position;
            spiderObject.transform.position = Vector3.MoveTowards(
                spiderObject.transform.position,
                target,
                chargeSpeed * Time.deltaTime
            );

            // Face the player while charging
            spiderObject.transform.LookAt(target);

            yield return null;
        }

        // Spider reached the player — cut to black here
        // (We'll set this up in Step 6)
        TriggerImpact();
    }

    void TriggerImpact()
    {
        // We'll connect this to the black screen in Step 6
        Debug.Log("IMPACT - Trigger your black screen here");
    }
}