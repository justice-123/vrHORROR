using System.Collections;
using UnityEngine;
using Bhaptics.SDK2;

public class PaperTouchHaptic : MonoBehaviour
{
    [Header("Haptic Events")]
    [SerializeField] AudioSource pianoScare;
    [SerializeField] private string heartbeatEvent = "hearthump";
    [SerializeField] private string jumpscareEvent = "jumpscare-back";

    [Header("Heartbeat Settings")]
    [SerializeField] private float heartbeatDuration = 10f;
    [SerializeField] private float breakBetweenBeats = 1f;

    [Header("Monster Settings")]
    [SerializeField] private GameObject monster;
    [SerializeField] private float spawnDistance = 2f;
    [SerializeField] private float turnThreshold = 150f;
    [SerializeField] private float faceDistance = 0.8f;
    [SerializeField] private float groundRayHeight = 2.5f;
    [SerializeField] private LayerMask groundMask = ~0; // set to your Ground layer if you have one
    [SerializeField] private float groundLift = 0.02f;  



    [Header("Player Head (recommended)")]
    [Tooltip("If left empty, uses Camera.main at runtime.")]
    [SerializeField] private Transform playerHead;

    private bool triggered = false;
    private Transform playerRoot;            
    private Vector3 initialForwardFlat;      

    void OnTriggerEnter(Collider other)
    {
        if (triggered) return;
        triggered = true;

        playerRoot = other.transform;

        // Use headset/camera for turning
        if (!playerHead)
        {
            if (Camera.main) playerHead = Camera.main.transform;
        }

        if (!playerHead)
        {
            Debug.LogError("[PaperTouchHaptic] No playerHead assigned and Camera.main not found.");
            return;
        }

        StartCoroutine(HorrorSequence());
    }

    IEnumerator HorrorSequence()
    {
        // heartbeat
        float timer = 0f;
        while (timer < heartbeatDuration)
        {
            BhapticsLibrary.Play(heartbeatEvent);
            yield return new WaitForSeconds(breakBetweenBeats);
            timer += breakBetweenBeats;
        }

        // Pause
        yield return new WaitForSeconds(6f);

        // Jumpscare haptic
        BhapticsLibrary.Play(jumpscareEvent);

        // Store head forward AFTER jumpscare (flattened)
        initialForwardFlat = Flatten(playerHead.forward);


        // Wait until player turns around
        yield return StartCoroutine(WaitForTurn());

        SpawnMonster();
    }

    IEnumerator WaitForTurn()
    {
        while (true)
        {
            Vector3 currentForwardFlat = Flatten(playerHead.forward);
            float angle = Vector3.Angle(initialForwardFlat, currentForwardFlat);

            // Debug (optional)
            // Debug.Log($"Turn angle: {angle}");

            if (angle >= turnThreshold)
                yield break;

            yield return null;
        }
    }

    void SpawnMonster()
    {
        if (!monster || !playerHead) return;

        // Spawn behind the direction you were facing at jumpscare time
        Vector3 spawnDir = -initialForwardFlat; 
        if (spawnDir.sqrMagnitude < 0.0001f) spawnDir = -Flatten(playerHead.forward);
        spawnDir.Normalize();

        Vector3 spawnPos = playerHead.position + spawnDir * faceDistance;

        // Raycast down to find ground height at spawn spot
        float groundY = spawnPos.y; // fallback
        Ray ray = new Ray(new Vector3(spawnPos.x, playerHead.position.y + groundRayHeight, spawnPos.z), Vector3.down);

        if (Physics.Raycast(ray, out RaycastHit hit, groundRayHeight + 5f, groundMask, QueryTriggerInteraction.Ignore))
        {
            groundY = hit.point.y;
        }

        monster.transform.position = new Vector3(spawnPos.x, groundY, spawnPos.z);
        monster.SetActive(true);

        
        Renderer rend = monster.GetComponentInChildren<Renderer>();
        if (rend != null)
        {
            float bottomY = rend.bounds.min.y;
            float offsetY = (groundY - bottomY) + groundLift;
            monster.transform.position += new Vector3(0f, offsetY, 0f);
        }

        // Rotate to stare at player (face-to-face)
        Vector3 lookDir = playerHead.position - monster.transform.position;
        lookDir.y = 0f;
        if (lookDir.sqrMagnitude > 0.0001f)
            monster.transform.rotation = Quaternion.LookRotation(lookDir);
        pianoScare.Play();
    }



    Vector3 Flatten(Vector3 v)
    {
        v.y = 0f;
        float mag = v.magnitude;
        return mag > 0.0001f ? (v / mag) : Vector3.forward;
    }
}
