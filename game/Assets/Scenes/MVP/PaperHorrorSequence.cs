using UnityEngine;
using System.Collections;
using Bhaptics.SDK2;

public class PaperHorrorSequence : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioClip scaryNoise;
    [SerializeField] private AudioClip eerieNoise;
    [SerializeField] private AudioClip jumpscareSound; // Optional

    [Header("Haptics - Use Exact Event Names")]
    [SerializeField] private string heartbeatEvent = "hearthump";
    [SerializeField] private string backScareEvent = "jumpscare-back";
    [SerializeField] private int heartbeatIntensity = 1;
    [SerializeField] private int heartbeatDuration = 200;

    [Header("Monster")]
    [SerializeField] private GameObject monster;
    [SerializeField] private float monsterDistanceBehindPlayer = 3f;

    [Header("Timing")]
    [SerializeField] private float silenceDuration = 7f;
    

    private AudioSource audioSource;
    private Transform playerTransform;
    private bool hasTriggered = false;
   

    void Start()
    {
        // Add audio source
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 1.0f; // 3D sound
        audioSource.minDistance = 1f;
        audioSource.maxDistance = 10f;

        // Find player camera (UPDATED LINE)
        Camera cam = FindFirstObjectByType<Camera>();
        if (cam != null)
        {
            playerTransform = cam.transform;
            Debug.Log("Player camera found!");
        }
        else
        {
            Debug.LogError("No camera found - horror sequence won't work!");
        }

        // Make sure monster is hidden
        if (monster != null)
        {
            monster.SetActive(false);
            Debug.Log("Monster hidden and ready");
        }
        else
        {
            Debug.LogWarning("No monster assigned! Drag the Monster cube into the script slot.");
        }

        Debug.Log("Horror sequence ready - touch the paper to begin!");
        Debug.Log("Using haptic events: " + heartbeatEvent + " and " + backScareEvent);
    }

    void OnTriggerEnter(Collider other)
    {
        // Trigger when player/hand/camera touches paper
        if (!hasTriggered)
        {
            Debug.Log("Collision detected with: " + other.gameObject.name + " (Tag: " + other.tag + ")");

            if (other.CompareTag("Player") || other.CompareTag("Hand") || other.CompareTag("MainCamera"))
            {
                Debug.Log("PAPER TOUCHED - STARTING HORROR SEQUENCE!");
                hasTriggered = true;
                StartCoroutine(HorrorSequence());
            }
        }
    }

    IEnumerator HorrorSequence()
    {
        Debug.Log("=== PHASE 1: Heartbeat for 10 seconds ===");

        float timer = 0f;

        while (timer < 10f) // 10 seconds total
        {
            BhapticsLibrary.Play(heartbeatEvent, heartbeatIntensity, heartbeatDuration);
            yield return new WaitForSeconds(1f); // 1 second between pulses
            timer += 1f;
        }

        Debug.Log("Heartbeat finished.");

        // 5 seconds silence
        Debug.Log("=== PHASE 2: Silence ===");
        yield return new WaitForSeconds(5f);

        Debug.Log("=== PHASE 3: JUMPSCARE ===");

        // Back tap event
        BhapticsLibrary.Play(backScareEvent, 1, 500);

        yield return new WaitForSeconds(0.3f);

        SpawnMonsterBehindPlayer();

        if (jumpscareSound != null)
        {
            audioSource.PlayOneShot(jumpscareSound);
        }
    }


    void SpawnMonsterBehindPlayer()
    {
        if (monster != null && playerTransform != null)
        {
            // Calculate position behind player
            Vector3 behindPlayer = playerTransform.position - (playerTransform.forward * monsterDistanceBehindPlayer);

            // Keep monster at reasonable height (eye level is scarier)
            behindPlayer.y = playerTransform.position.y;

            // Place monster
            monster.transform.position = behindPlayer;

            // Make monster face the player
            monster.transform.LookAt(new Vector3(playerTransform.position.x, monster.transform.position.y, playerTransform.position.z));

            // Activate monster
            monster.SetActive(true);

            Debug.Log("Monster spawned behind player at: " + behindPlayer);
        }
        else
        {
            if (monster == null)
                Debug.LogError("Cannot spawn monster - no monster assigned!");
            if (playerTransform == null)
                Debug.LogError("Cannot spawn monster - no player transform found!");
        }
    }

    // Manual test trigger - press T to test without touching paper
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T) && !hasTriggered)
        {
            Debug.Log("===== MANUAL TRIGGER (T key) - Testing horror sequence =====");
            hasTriggered = true;
            StartCoroutine(HorrorSequence());
        }

        // Press H to test just the heartbeat
        if (Input.GetKeyDown(KeyCode.H))
        {
            Debug.Log("Testing hearthump event only");
            BhapticsLibrary.Play(heartbeatEvent, heartbeatIntensity, heartbeatDuration);
        }

        // Press B to test just the back scare
        if (Input.GetKeyDown(KeyCode.B))
        {
            Debug.Log("Testing jumpscare-back event only");
            BhapticsLibrary.Play(backScareEvent, 1, 500);
        }
    }
}