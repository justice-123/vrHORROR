using UnityEngine;
using UnityEngine.AI;

public class MonsterAI : MonoBehaviour
{
    public Transform player;
    private float viewAngle = 55f;
    private float viewRange = 40f;
    private float moveThreshold = 0.1f; // For player

    [Header("Colors")]
    public Color wanderingColor = Color.green;
    public Color canSeeColor = Color.white;
    public Color huntingColor = Color.red;

    private MeshRenderer meshRenderer;

    private NavMeshAgent agent;
    private Vector3 lastPlayerPos;


    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        meshRenderer = GetComponent<MeshRenderer>();


        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");

        if (playerObj != null)
        {
            player = playerObj.transform;
            lastPlayerPos = player.position;
        }
        else
        {
            Debug.LogError("Monster can't find the Player");
        }

        lastPlayerPos = player.position;
        Wander();
    }

    void Update()
    {
        float distance = Vector3.Distance(transform.position, player.position); //checks current monster distance to player
        float playerSpeed = (player.position - lastPlayerPos).magnitude / Time.deltaTime;

        if (CanSeePlayer())
        {
            Debug.Log("Can See Player");

            // If player moves while in view, move closer
            if (playerSpeed > moveThreshold)
            {
                agent.SetDestination(player.position);
                agent.speed = 3.0f;

                ChangeColor(huntingColor);
            }
            else
            {
                ChangeColor(canSeeColor);

                agent.speed = 1.5f;  // keep moving closer to player, but go elsewhere once near player 
                if (!agent.hasPath || agent.remainingDistance < 0.5f) Wander();


            }
        }
        else
        {
            // Randomly wander 
            ChangeColor(wanderingColor);
            if (!agent.hasPath || agent.remainingDistance < 0.5f) Wander();
        }

        // Check for Fail State
        if (distance < 1.5f) Debug.Log("Game Over!");

        lastPlayerPos = player.position;



    }

    bool CanSeePlayer()
    {
        Vector3 rayOrigin = transform.position + Vector3.up * 0.5f;
        Vector3 directionToPlayer = (player.position - rayOrigin).normalized;
        if (Vector3.Angle(transform.forward, directionToPlayer) < viewAngle)
        {
            if (Physics.Raycast(transform.position, directionToPlayer, out RaycastHit hit, viewRange))
            {
                return hit.transform.CompareTag("Player");
            }
        }
        return false;
    }


    void Wander() 
    { 
  
        agent.speed = 1.5f;
        Vector3 randomPoint = transform.position + Random.insideUnitSphere * 10f;
        if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, 10f, 1))
        {
            agent.SetDestination(hit.position);
        }
    }

    void ChangeColor(Color newColor)
    {
        if (meshRenderer != null)
        {
            meshRenderer.material.color = newColor;
        }
    }

    // REMOVE THIS I BEG
    void OnDrawGizmos()
    {
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, viewRange);

        Gizmos.color = Color.red;

        Vector3 leftBoundary = Quaternion.AngleAxis(-viewAngle, Vector3.up) * transform.forward;
        Vector3 rightBoundary = Quaternion.AngleAxis(viewAngle, Vector3.up) * transform.forward;

        Gizmos.DrawLine(transform.position, transform.position + leftBoundary * viewRange);
        Gizmos.DrawLine(transform.position, transform.position + rightBoundary * viewRange);
    }

}