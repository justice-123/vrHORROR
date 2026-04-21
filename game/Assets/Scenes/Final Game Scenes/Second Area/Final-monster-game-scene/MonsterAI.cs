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
    public float moveThreshold = 0.12f; // For player
    public float eyeHeight = 0.5f;

    [Header("BHaptics")]
    public string heartbeatClip = "Heartbeat";
    public string huntClip = "Hunt_Vibration";

    //[Header("Colors")]
    //public Color wanderingColor = Color.green;
    //public Color canSeeColor = Color.white;
    //public Color huntingColor = Color.red;

    private MeshRenderer meshRenderer;

    private NavMeshAgent agent;
    private Vector3 lastPlayerPos, lastLeftPos, lastRightPos;
    private float hapticTimer;


    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        meshRenderer = GetComponent<MeshRenderer>();


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

                agent.SetDestination(player.position);
                agent.speed = 3.5f;

                //ChangeColor(huntingColor);
                HandleHaptics(distance, true);
            }
            else
            {
                //ChangeColor(canSeeColor);
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
            if (!agent.hasPath || agent.remainingDistance < 0.5f) Wander();
            BhapticsLibrary.StopAll();
        }

        // Check for Fail State
        if (distance < 1.5f) Debug.Log("Game Over!");

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
        float interval = Mathf.Clamp(distance / 15f, 0.2f, 1.0f);

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
            if (hapticTimer >= 0.25f)
            {
                BhapticsLibrary.Play(heartbeatClip);
                hapticTimer = 0;
            }
        }

        else  // player still
        {
            BhapticsLibrary.StopByEventId(huntClip);

            if (hapticTimer >= interval)
            {
                float angle = CalculatePlayerAngle();
                BhapticsLibrary.PlayAngle(heartbeatClip, angle, 0f);
                hapticTimer = 0;
            }
        }



    }

    float CalculatePlayerAngle()
    {
        Vector3 dirToMonster = (transform.position - player.position).normalized;
        float angle = Vector3.SignedAngle(player.forward, dirToMonster, Vector3.up);
        if (angle < 0) angle += 360f;
        return angle;
    }

    bool CanSeePlayer()
    {
        Vector3 rayOrigin = transform.position + Vector3.up * 0.5f;
        Vector3 directionToPlayer = (player.position - rayOrigin).normalized;
        if (Vector3.Angle(transform.forward, directionToPlayer) < viewAngle)
        {
            if (Physics.Raycast(transform.position, directionToPlayer, out RaycastHit hit, viewRange))
            {
                if (hit.transform != null)
                    Debug.Log("Monster ray hit: " + hit.transform.name + " with tag: " + hit.transform.tag);


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