using UnityEngine;
using UnityEngine.AI;
using Bhaptics.SDK2;
using UnityEngine.XR;
//using System.Collections.Generic;

public class MonsterAI : MonoBehaviour
{
    private Transform player;
    private Transform leftHand;
    private Transform rightHand;


    [Header("Sensitivity")]
    private float viewAngle = 20f;
    private float viewRange = 25f;
    private float moveThreshold = 0.12f; // For player
    private float eyeHeight = 1.5f;

    [Header("Chase Delay")]
    private float chaseDelay = 1.0f; // How long player can move before being chased
    private float movementTimer = 0f;

    [Header("BHaptics")]
    private string heartbeatClip = "heartbeat_buzz";
    private string huntClip = "hunt_vibration";

    //[Header("Colors")]
    //public Color wanderingColor = Color.green;
    //public Color canSeeColor = Color.white;
    //public Color huntingColor = Color.red;

    private MeshRenderer meshRenderer;

    private NavMeshAgent agent;
    private Animator anim;
    private Vector3 lastPlayerPos, lastLeftPos, lastRightPos;
    private float hapticTimer;

    private bool isGameOver = false;


    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        meshRenderer = GetComponent<MeshRenderer>();
        anim = GetComponent<Animator>();

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


        float distance = Vector3.Distance(transform.position, player.position); //checks current monster distance to player
        //float playerSpeed = (player.position - lastPlayerPos).magnitude / Time.deltaTime;

        bool monsterSeesPlayer = CanSeePlayer();
        float totalMotion = CalculateCombinedMotion();

        anim.SetFloat("Speed", agent.velocity.magnitude);

        if (monsterSeesPlayer)
        {


            // If player moves while in view, move closer
            if (totalMotion > moveThreshold)
            {
                Debug.Log("Can See Player and hunting");

                movementTimer += Time.deltaTime;
                if (movementTimer >= chaseDelay) { 

                    agent.SetDestination(player.position);
                    agent.speed = 3.5f;

                    //ChangeColor(huntingColor);
                    HandleHaptics(distance, true);
                }
            }
            else
            {
                //ChangeColor(canSeeColor);
                Debug.Log("Can See Player");
                HandleHaptics(distance, false);

                agent.speed = 1.0f;  // keep moving closer to player, but go elsewhere once near player 
                if (!agent.hasPath || agent.remainingDistance < 2f) Wander();


            }
        }
        else
        {
            movementTimer = 0f;

            // Randomly wander 
            //ChangeColor(wanderingColor);
            if (!agent.hasPath || agent.remainingDistance < 1.0f) Wander();
            //BhapticsLibrary.StopAll();
        }

        // Check for Fail State
        if (distance < 1.0f)
        {
            BhapticsLibrary.StopAll();
            //if (BhapticsLibrary.IsPlaying(huntClip)) BhapticsLibrary.Stop(huntClip);
            Debug.Log("Game Over!");

            isGameOver = false;
            agent.isStopped = true; 
            anim.SetTrigger("Attack");

            BhapticsLibrary.Play("DeathImpact");
        }

        //lastPlayerPos = player.position;
        UpdateLastPositions();



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
        float interval = Mathf.Clamp(distance / 15f, 0.1f, 1.0f);

        if (isHunting)
        {
            // Full Vest Rumble

            BhapticsLibrary.Play(huntClip);
            //if (!BhapticsLibrary.IsPlayingByEventId(huntClip)) BhapticsLibrary.PlayAngle(huntClip, angle, 0f);
            //if (!BhapticsLibrary.IsPlaying()) BhapticsLibrary.Play(huntClip);

            // Controller 
            var leftHand = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            var rightHand = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);

            leftHand.SendHapticImpulse(0, 1f, 0.1f);
            rightHand.SendHapticImpulse(0, 1f, 0.1f);

            // Heartbeat all directions
            //if (hapticTimer >= 0.25f)
            //{
            //    BhapticsLibrary.Play(heartbeatClip, 0 , 1 , 0.1f,  angle, 0f);
            //    //BhapticsLibrary.Play(heartbeatClip);
            //    hapticTimer = 0;
            //}
        }

        else  // player still
        {
            BhapticsLibrary.StopByEventId(huntClip);

            if (hapticTimer >= interval)
            {
                float angle = CalculatePlayerAngle();
                BhapticsLibrary.Play(heartbeatClip, 0 , 1 , 0.1f,  angle, 0f);
                //BhapticsLibrary.Play(heartbeatClip);
                hapticTimer = 0;
            }
        }



    }

    float CalculatePlayerAngle()
    {
        Vector3 dirToMonster = (transform.position - player.position).normalized;
        float angle = Vector3.SignedAngle(player.forward, dirToMonster, Vector3.up);
        angle = -angle;
        if (angle < 0) angle += 360f;
        return angle;
    }

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


                return hit.transform.CompareTag("MainCamera");
                //return hit.transform.CompareTag("Player");
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

    void ChangeColor(Color newColor)
    {
        if (meshRenderer != null)
        {
            meshRenderer.material.color = newColor;
        }
    }

    void OnDrawGizmos()
    {
        // 1. Calculate the eye position (must match your CanSeePlayer logic)
        Vector3 rayOrigin = transform.position + Vector3.up * eyeHeight;

        // 2. Draw the View Range (Yellow Sphere)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(rayOrigin, viewRange);

        // 3. Draw the Field of View "Cone" (Red Lines)
        Gizmos.color = Color.red;

        // Calculate the left and right edges of the vision cone
        Vector3 leftBoundary = Quaternion.AngleAxis(-viewAngle, Vector3.up) * transform.forward;
        Vector3 rightBoundary = Quaternion.AngleAxis(viewAngle, Vector3.up) * transform.forward;

        // Draw the lines starting from the EYES
        Gizmos.DrawLine(rayOrigin, rayOrigin + leftBoundary * viewRange);
        Gizmos.DrawLine(rayOrigin, rayOrigin + rightBoundary * viewRange);

        // 4. Visual Debug: Draw a line to the player if they are found
        if (player != null)
        {
            float distance = Vector3.Distance(transform.position, player.position);

            // If within range and angle, show the "target" line
            Vector3 dirToPlayer = (player.position - rayOrigin).normalized;
            if (distance < viewRange && Vector3.Angle(transform.forward, dirToPlayer) < viewAngle)
            {
                Gizmos.color = Color.green; // Monster is looking at player
                Gizmos.DrawLine(rayOrigin, player.position);
            }
        }
    }

}