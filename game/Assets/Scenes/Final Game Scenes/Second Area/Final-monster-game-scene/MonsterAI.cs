using UnityEngine;
using UnityEngine.AI;
using Bhaptics.SDK2;
using UnityEngine.XR;
//using System.Collections.Generic;

public class MonsterAI : MonoBehaviour
{
    public Transform player;
    public Transform leftHand;
    public Transform rightHand;


    [Header("Sensitivity")]
    public float viewAngle = 20f;
    public float viewRange = 25f;
    public float moveThreshold = 0.15f; // For player
    public float eyeHeight = 0.5f;

    [Header("BHaptics")]
    public string heartbeatClip = "heartbeat_buzz";
    public string huntClip = "hunt_vibration";

    [Header("Chase Settings")]
    public float chaseDelay = 1.0f;
    private float movementTimer = 0f;

    //[Header("Colors")]
    //public Color wanderingColor = Color.green;
    //public Color canSeeColor = Color.white;
    //public Color huntingColor = Color.red;

    //private MeshRenderer meshRenderer;

    private NavMeshAgent agent;
    private Animator anim;
    private Vector3 lastPlayerPos, lastLeftPos, lastRightPos;
    private float hapticTimer;
    private bool isGameOver = false;

    private bool initialized = false;


    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        //meshRenderer = GetComponent<MeshRenderer>();


        GameObject playerObj = GameObject.FindGameObjectWithTag("MainCamera");
        GameObject lHandObj = GameObject.FindGameObjectWithTag("Left Controller");
        GameObject rHandObj = GameObject.FindGameObjectWithTag("Right Controller");

        if (playerObj != null && lHandObj != null && rHandObj != null)
        {
            player = playerObj.transform;
            leftHand = lHandObj.transform;
            rightHand = rHandObj.transform;

            UpdateLastPositions();
        }
        else
        {
            Debug.LogError("Monster can't find the Player");
        }

        //lastPlayerPos = player.position;
        Wander();
    }

    void Update()
    {
        if (isGameOver) return;
        UpdateLastPositions();


        float distance = Vector3.Distance(transform.position, player.position); //checks current monster distance to player
        //float playerSpeed = (player.position - lastPlayerPos).magnitude / Time.deltaTime;
        bool monsterSeesPlayer = CanSeePlayer();
        float totalMotion = CalculateCombinedMotion();

        if (monsterSeesPlayer)
        {
            

            // If player moves while in view, move closer
            if (totalMotion > moveThreshold)
            {
                Debug.Log("Can See Player and hunting");

                movementTimer += Time.deltaTime;
                if (movementTimer >= chaseDelay)
                {
                    agent.SetDestination(player.position);
                    agent.speed = 3.5f;

                    //ChangeColor(huntingColor);
                    HandleHaptics(distance, true);
                }

            }
            else
            {
                movementTimer = 0f;
                Debug.Log("Can See Player");
                HandleHaptics(distance, false);

                agent.speed = 1.5f;  // keep moving closer to player, but go elsewhere once near player 
                if (!agent.hasPath || agent.remainingDistance < 2f) Wander();


            }
        }
        else
        {
            // Randomly wander 
            //ChangeColor(wanderingColor);
            movementTimer = 0f;
            //BhapticsLibrary.StopAll();
            if (!agent.hasPath || agent.remainingDistance < 1f) Wander();
        }

        // Check for Fail State
        if (distance < 0.5f) {
            Debug.Log("Game Over!");
            isGameOver = true;
            agent.isStopped = true;
        }

        //lastPlayerPos = player.position;

    }

    float CalculateCombinedMotion()
    {
        float headM = (player.position - lastPlayerPos).magnitude;
        float leftM = (leftHand.position - lastLeftPos).magnitude;
        float rightM = (rightHand.position - lastRightPos).magnitude;

        // Sum of all movement 
        return (headM + leftM + rightM) / Time.deltaTime;
    }

    void UpdateLastPositions()
    {
        lastPlayerPos = player.position;
        lastLeftPos = leftHand.position;
        lastRightPos = rightHand.position;
    }

    void HandleHaptics(float distance, bool isHunting)
    {
        hapticTimer += Time.deltaTime;

        // Heartbeat  gets faster as monster gets closer
        float interval = Mathf.Clamp(distance / 10f, 0.4f, 1.0f);
        Debug.Log(interval);
        Debug.Log("Distance: " + distance);

        if (isHunting)
        {
            // Full Vest Rumble
            if (!BhapticsLibrary.IsPlayingByEventId(huntClip)) BhapticsLibrary.Play(huntClip);
            //if (!BhapticsLibrary.IsPlaying()) BhapticsLibrary.Play(huntClip);

            // Controller 
            var leftHand = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            var rightHand = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);

            leftHand.SendHapticImpulse(0, 0.8f, 0.1f);
            rightHand.SendHapticImpulse(0, 0.8f, 0.1f);

            // Heartbeat all directions
            //if (hapticTimer >= 0.25f)
            //{
                //BhapticsLibrary.Play(heartbeatClip);
                //hapticTimer = 0;
            //}
        }

        else  // player still
        {
            BhapticsLibrary.StopByEventId(huntClip);

            if (hapticTimer >= interval)
            {
                Vector3 dirToMonster = (transform.position - player.position).normalized;
                float angle = -Vector3.SignedAngle(player.forward, dirToMonster, Vector3.up);
                if (angle < 0) angle += 360f;
                if (!BhapticsLibrary.IsPlayingByEventId(heartbeatClip)) BhapticsLibrary.PlayAngle(heartbeatClip, angle, 0f);
                hapticTimer = 0;



            }
        }



    }

    //float CalculatePlayerAngle()
    //{
    //    Vector3 dirToMonster = (transform.position - player.position).normalized;
    //    float angle = Vector3.SignedAngle(player.forward, dirToMonster, Vector3.up);
    //    if (angle < 0) angle += 360f;
    //    return angle;
    //}

    bool CanSeePlayer()
    {
        Vector3 rayOrigin = transform.position + Vector3.up * eyeHeight;
        Vector3 directionToPlayer = (player.position - rayOrigin).normalized;
        if (Vector3.Angle(transform.forward, directionToPlayer) < viewAngle)
        {
            if (Physics.Raycast(rayOrigin, directionToPlayer, out RaycastHit hit, viewRange))
            {
                if (hit.transform != null)
                    Debug.Log("Monster ray hit: " + hit.transform.name + " with tag: " + hit.transform.tag);


                return hit.transform.CompareTag("Player") || hit.transform.CompareTag("MainCamera") || hit.transform == player;
            }
        }
        return false;
    }


    void Wander() 
    { 
  
        agent.speed = 1.5f;
        Vector3 randomPoint = transform.position + Random.insideUnitSphere * 10f;
        if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, 10f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }

    //void ChangeColor(Color newColor)
    //{
    //    if (meshRenderer != null)
    //    {
    //        meshRenderer.material.color = newColor;
    //    }
    //}

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