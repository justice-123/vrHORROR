using UnityEngine;
using System.Collections;
using Bhaptics.SDK2;

public class PaperHorrorSequence : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioClip scaryNoise;
    [SerializeField] private AudioClip eerieNoise;
    [SerializeField] private AudioClip jumpscareSound;

    [Header("Haptics")]
    [SerializeField] private string heartbeatEvent = "hearthump";
    [SerializeField] private string backScareEvent = "jumpscare-back";
    [SerializeField] private int heartbeatIntensity = 1;
    [SerializeField] private int heartbeatDuration = 200;

    [Header("Monster")]
    [SerializeField] private GameObject monster;
    [SerializeField] private float monsterDistanceFromPlayer = 2f;

    [Header("Timing")]
    [SerializeField] private float heartbeatTotalTime = 10f;
    [SerializeField] private float heartbeatInterval = 1f;
    [SerializeField] private float silenceDuration = 5f;

    private AudioSource audioSource;
    private Transform playerTransform;
    private bool hasTriggered = false;

    void Start()
    {
        // setting up audiosource
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 1f;
        audioSource.minDistance = 1f;
        audioSource.maxDistance = 15f;

        // Finding the VR camera
        Camera cam = FindFirstObjectByType<Camera>();
        if (cam != null)
        {
            playerTransform = cam.transform;
            Debug.Log("Player camera found.");
        }
        else
        {
            Debug.LogError("No camera found in scene!");
        }

        // Hide monster at start
        if (monster != null)
        {
            monster.SetActive(false);
            Debug.Log("Monster hidden.");
        }
        else
        {
            Debug.LogWarning("No monster assigned in Inspector.");
        }

        Debug.Log("Horror sequence ready.");
    }

    void OnTriggerEnter(Collider other)
    {
        if (!hasTriggered)
        {
            if (other.CompareTag("Player") ||
                other.CompareTag("MainCamera") ||
                other.CompareTag("Hand"))
            {
                hasTriggered = true;
                Debug.Log("Paper touched. Starting horror sequence.");
                StartCoroutine(HorrorSequence());
            }
        }
    }

    IEnumerator HorrorSequence()
    {
        Debug.Log("heartbeat activated");

        
        if (scaryNoise != null)
            audioSource.PlayOneShot(scaryNoise);

        float timer = 0f;

        while (timer < heartbeatTotalTime)
        {
            BhapticsLibrary.Play(heartbeatEvent, heartbeatIntensity, heartbeatDuration);
            yield return new WaitForSeconds(heartbeatInterval);
            timer += heartbeatInterval;
        }

        Debug.Log("Heartbeat complete.");

        
        if (eerieNoise != null)
            audioSource.PlayOneShot(eerieNoise);

       
        yield return new WaitForSeconds(silenceDuration);

        Debug.Log("back tap");

        // Strong back tap
        BhapticsLibrary.Play(backScareEvent, 1, 600);

        yield return new WaitForSeconds(0.8f);

        // Wait for player to turn
        StartCoroutine(WaitForPlayerTurn());
    }

    IEnumerator WaitForPlayerTurn()
    {
       // Debug.Log("Waiting for player to turn around...");

        if (playerTransform == null)
            yield break;

        // Store initial horizontal forward direction
        Vector3 initialForward = playerTransform.forward;
        initialForward.y = 0f;
        initialForward.Normalize();

        while (true)
        {
            Vector3 currentForward = playerTransform.forward;
            currentForward.y = 0f;
            currentForward.Normalize();

            float angle = Vector3.Angle(initialForward, currentForward);

            

            if (angle > 120f) // Adjust if needed
            {
                Debug.Log("Player turned enough!");
                SpawnMonsterInFrontOfPlayer();
                yield break;
            }

            yield return null;
        }
    }

    void SpawnMonsterInFrontOfPlayer()
    {
        if (monster == null || playerTransform == null)
            return;

        // Get player horizontal forward direction only
        Vector3 forward = playerTransform.forward;
        forward.y = 0f;
        forward.Normalize();

        // Get floor height from monster's current position
        float groundY = monster.transform.position.y;

        // Calculate spawn position
        Vector3 spawnPosition = playerTransform.position + forward * monsterDistanceFromPlayer;

        // Force monster to stay on floor
        spawnPosition.y = groundY;

        monster.transform.position = spawnPosition;

        // Make monster face player horizontally
        Vector3 lookTarget = playerTransform.position;
        lookTarget.y = spawnPosition.y;

        monster.transform.LookAt(lookTarget);

        monster.SetActive(true);

        Debug.Log("Monster spawned correctly on floor.");
    }


    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T) && !hasTriggered)
        {
            hasTriggered = true;
            StartCoroutine(HorrorSequence());
        }
    }
}
