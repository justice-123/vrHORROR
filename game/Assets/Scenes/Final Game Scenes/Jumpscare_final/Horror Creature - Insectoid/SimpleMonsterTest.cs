using UnityEngine;

public class SimpleMonsterTest : MonoBehaviour
{
    public GameObject monsterToSpawn;
    public Transform playerCamera;

    private bool hasTriggered = false;

    void Start()
    {
        Debug.LogWarning(" SimpleMonsterTest is running");

        // Auto-find camera if not assigned
        if (playerCamera == null && Camera.main != null)
        {
            playerCamera = Camera.main.transform;
            Debug.LogWarning(" Found camera: " + playerCamera.name);
        }

        // Auto-find monster if not assigned
        if (monsterToSpawn == null)
        {
            monsterToSpawn = GameObject.Find("Insectoid (1)");
            if (monsterToSpawn == null) monsterToSpawn = GameObject.Find("Insectoid");
            Debug.LogWarning(" Found monster: " + (monsterToSpawn != null ? monsterToSpawn.name : "NONE"));
        }

        // Start with monster hidden
        if (monsterToSpawn != null) monsterToSpawn.SetActive(false);
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.LogWarning(" TEST TRIGGER HIT BY: " + other.name + " TAG: " + other.tag);

        if (hasTriggered) return;

        if (other.CompareTag("Player"))
        {
            hasTriggered = true;
            Debug.LogWarning("PLAYER HIT TEST TRIGGER - SPAWNING MONSTER");
            SpawnMonsterInFront();
        }
    }

    void SpawnMonsterInFront()
    {
        if (monsterToSpawn == null || playerCamera == null)
        {
            Debug.LogWarning(" Missing monster or camera - cannot spawn");
            return;
        }

        // Position monster 3 metres in front of the player
        Vector3 forward = playerCamera.forward;
        forward.y = 0;
        forward.Normalize();

        Vector3 spawnPos = playerCamera.position + forward * 3f;
        spawnPos.y = playerCamera.position.y - 1f; // Ground level roughly

        monsterToSpawn.transform.position = spawnPos;
        monsterToSpawn.transform.LookAt(playerCamera.position);

        // Enable it
        monsterToSpawn.SetActive(true);

        // Play attack animation
        Animator anim = monsterToSpawn.GetComponent<Animator>();
        if (anim != null)
        {
            anim.SetTrigger("Attack");
            Debug.LogWarning(" Attack triggered!");
        }
        else
        {
            Debug.LogWarning(" No animator found on monster");
        }
    }
}