using UnityEngine;
using UnityEngine.AI;
using Bhaptics.SDK2;
using UnityEngine.XR;
using System.Collections;
using UnityEngine.Rendering;



public class MonsterAI : MonoBehaviour
{
    private Transform player;
    private Transform leftHand;
    private Transform rightHand;


    [Header("Sensitivity")]
    private float viewAngle = 30f;
    private float viewRange = 25f;
    private float moveThreshold = 0.25f; // For player
    public float eyeHeight = 0.5f;
    private float deadzone = 0.001f;
    private float personalSpace = 2.0f;


    [Header("BHaptics")]
    private string heartbeatClip = "heartbeat_buzz";
    private string huntClip = "hunt_vibration";
    private string monster_slash = "monster-slash";

    [Header("Chase Settings")]
    private float chaseDelay = 0.25f;
    private float movementTimer = 0f;
    private float chaseSpeed = 4.0f;

    [Header("Wander Settings")]
    private float pauseDuration = 1.7f; // stare lenght
    private float pauseTimer = 0f;
    private bool isPausing = false;

    [Header("Anti-Stuck")]
    public float stuckThreshold = 3.0f; 
    private float stuckTimer = 0f;

    [Header("Audio")]
    public AudioSource movementSource; // For footsteps
    public AudioSource vocalSource;    // For Aggro and Attack
    public AudioClip walkClip;
    public AudioClip aggroClip;
    public AudioClip attackClip;


    public GameObject attackVolumeObj;
    public GameObject blackoutVolumeObj;
    private Volume attackVolume;
    private Volume blackoutVolume;

    private NavMeshAgent agent;
    private Animator anim;
    private Vector3 lastPlayerPos, lastLeftPos, lastRightPos;
    private float hapticTimer;
    private bool isGameOver = false;
    private bool isAttacking = false;

    private bool initialized = false;

    private UnityEngine.XR.InputDevice leftHandHap;
    private UnityEngine.XR.InputDevice rightHandHap;

    private OxygenTank playerOxygen;

    private GameObject playerWheelchair;




    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponentInChildren<Animator>();
        //meshRenderer = GetComponent<MeshRenderer>();


        GameObject playerObj = GameObject.FindGameObjectWithTag("MainCamera");
        GameObject lHandObj = GameObject.FindGameObjectWithTag("Left Controller");
        GameObject rHandObj = GameObject.FindGameObjectWithTag("Right Controller");
        playerWheelchair = GameObject.FindGameObjectWithTag("Player");



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

        leftHandHap = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        rightHandHap = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);


        GameObject OxyTank = GameObject.FindGameObjectWithTag("oxygentank");
        playerOxygen = OxyTank.GetComponent<OxygenTank>();

        if (playerOxygen == null)
        {
            Debug.LogError("ox_tank not found");
        }

        //attackVolume = GameObject.Find("Damage Volume");
        
        
        attackVolume = attackVolumeObj.GetComponent<Volume>();
        blackoutVolume = blackoutVolumeObj.GetComponent<Volume>();


        UpdateLastPositions();
        Wander();
    }

    void Update()
    {
        if (isGameOver || isAttacking) 
        {
            StopWalkingSound();
            return;
        }
        UpdateWalkingSound();


        float distance = Vector3.Distance(transform.position, player.position); //checks current monster distance to player
        //float playerSpeed = (player.position - lastPlayerPos).magnitude / Time.deltaTime;
        bool monsterSeesPlayer = CanSeePlayer();
        float totalMotion = CalculateCombinedMotion();

        if (monsterSeesPlayer)
        {
            

            // If player moves while in view, move closer
            if (totalMotion > moveThreshold)
            {
                Debug.Log("Hunting");
                //UpdateWalkingSound(chaseSpeed);
                UpdateWalkingSound();

                isPausing = false;
                agent.isStopped = false;

                movementTimer += Time.deltaTime;
                if (movementTimer >= chaseDelay)
                {
                    HandleHaptics(distance, true);

                    agent.SetDestination(player.position);
                    agent.speed = chaseSpeed;

                    //ChangeColor(huntingColor);
                }

            }
            else
            {
                //movementTimer = 0f;
                Debug.Log("Can See Player, but theyre still");
                HandleHaptics(distance, false);

                agent.speed = 1.0f;  // keep moving closer to player, 

                //if (distance < personalSpace)
                //{
                //    if (!isPausing && Vector3.Distance(agent.destination, player.position) < personalSpace)
                //    {
                //        WanderAwayFromPlayer();
                //    }
                //    else if (!agent.pathPending && agent.remainingDistance < 0.75f)
                //    {
                //        WanderAwayFromPlayer();
                //    }
                //}

                if (distance < personalSpace)
                {
                    if (!isPausing && Vector3.Distance(agent.destination, player.position) < personalSpace)
                    {
                        isPausing = true;
                        pauseTimer = 0f;

                        agent.isStopped = true;
                        agent.ResetPath();
                        agent.velocity = Vector3.zero;

                        anim.SetTrigger("Aggro");
                        vocalSource.PlayOneShot(aggroClip);
                        StopWalkingSound();
                    }

                    if (isPausing)
                    {
                        pauseTimer += Time.deltaTime;


                        if (pauseTimer >= pauseDuration)
                        {
                            isPausing = false;
                            agent.isStopped = false;
                            WanderAwayFromPlayer();
                        }
                    }
                    else if (!agent.pathPending && agent.remainingDistance < 0.75f)
                    {
                        //UpdateWalkingSound(1.0f);
                        UpdateWalkingSound();
                        WanderAwayFromPlayer();
                    }
                }


                else
                {
                    // wander normally
                    isPausing = false;
                    agent.isStopped = false;
                    agent.speed = 1.0f;

                    if (!agent.pathPending && agent.remainingDistance < 0.75f)
                    {
                        Wander();
                        //UpdateWalkingSound(1.0f);
                        UpdateWalkingSound();
                    }
                }


                //if (!agent.pathPending && agent.remainingDistance < 0.75f) Wander();

                //if (distance < 1) { 
                //    Wander(); 
                //}


            }
        }
        else
        {

            // Randomly wander 
            //UpdateWalkingSound(1.0f);
            UpdateWalkingSound();
            agent.speed = 1f;
            movementTimer = 0f;
            //BhapticsLibrary.StopAll();
            if (!agent.pathPending && agent.remainingDistance < 0.75f) Wander();
        }

        // Check for Fail State
        if (distance < 1.5f && !isAttacking) {
            StartCoroutine(AttackAndFlee());
        }




        // Anti-stuck stuff
        if (!agent.isStopped && !isPausing && !isAttacking && !isGameOver)
        {
            if (agent.velocity.magnitude < 0.2f)
            {
                stuckTimer += Time.deltaTime;
                if (stuckTimer >= stuckThreshold)
                {
                    Debug.Log("Monster is stuck");
                    Wander(); 
                    stuckTimer = 0f;
                }
            }
            else
            {
                stuckTimer = 0f;
            }
        }
        else
        {
            stuckTimer = 0f;
        }




        anim.SetFloat("Speed", agent.velocity.magnitude);
        UpdateLastPositions();


    }

    private IEnumerator AttackAndFlee()
    {
        isAttacking = true;

        Debug.Log("Attacking Player");
        //isGameOver = true;
        StopWalkingSound();



        agent.speed = 0f;
        agent.isStopped = true;
        agent.ResetPath();
        agent.velocity = Vector3.zero;
        anim.SetFloat("Speed", 0f);

        Vector3 lookPos = player.position;
        lookPos.y = transform.position.y;
        transform.LookAt(lookPos);
        anim.SetTrigger("Attack");
        vocalSource.PlayOneShot(attackClip);

        // Visuals
        attackVolume.weight = 1f;

        BhapticsLibrary.StopAll();
        StartCoroutine(PlayHapticForDuration(huntClip, 2.0f, 0.1f));
        leftHandHap.SendHapticImpulse(0, 1.0f, 2f);
        rightHandHap.SendHapticImpulse(0, 1.0f, 2f);
        //BhapticsLibrary.Play(monster_slash);

        // slowly fade away red
        //float fadeTime = 3.0f;
        //float startWeight = 1f;
        //for (float t = 0; t < fadeTime; t += Time.deltaTime)
        //{
        //    attackVolume.weight = Mathf.Lerp(startWeight, 0f, t / fadeTime);
        //    yield return null;
        //}
        //attackVolume.weight = 0f;

        if (playerOxygen != null)
        {
            playerOxygen.UseOxygen(30f);
            Debug.Log($"Oxygen remaining: {playerOxygen.oxygenLevel}%");
        }

        yield return new WaitForSeconds(2.0f);

        // fade to black
        float fadeToBlackTime = 1.5f;
        for (float t = 0; t < fadeToBlackTime; t += Time.deltaTime)
        {
            blackoutVolume.weight = t / fadeToBlackTime;
            yield return null;
        }
        blackoutVolume.weight = 1f;


        // monster runs away
        agent.isStopped = false;
        agent.speed = chaseSpeed;
        WanderAwayFromPlayer();
        //UpdateWalkingSound(chaseSpeed);
        UpdateWalkingSound();



        yield return new WaitForSeconds(0.5f);
        isAttacking = false;


        float finalFade = 2.5f;
        for (float t = 0; t < finalFade; t += Time.deltaTime)
        {
            blackoutVolume.weight = 1f - (t / finalFade);
            attackVolume.weight = 1f - (t / finalFade);

            yield return null;
        }

        blackoutVolume.weight = 0f;
        attackVolume.weight = 0f;




    }

    void UpdateWalkingSound()
    {
        //if (agent.velocity.magnitude > 0.2f && !agent.isStopped)
        //{
        //    if (!movementSource.isPlaying)
        //    {
        //        movementSource.clip = walkClip;
        //        movementSource.Play();
        //    }

        //    movementSource.pitch = pitch;
        //}
        //else
        //{
        //    StopWalkingSound();
        //}


        float currentVelocity = agent.velocity.magnitude;

        if (currentVelocity > 0.1f && !agent.isStopped)
        {
            if (!movementSource.isPlaying)
            {
                movementSource.clip = walkClip;
                movementSource.Play();
            }
            //movementSource.pitch = currentVelocity;

            float minPitch = 1f;
            float maxPitch = 2f;

            float speedPercentage = currentVelocity / chaseSpeed;

            movementSource.pitch = Mathf.Lerp(minPitch, maxPitch, speedPercentage);
            //movementSource.volume = Mathf.Lerp(0.7f, 1.0f, speedPercentage);

        }
        else
        {
            StopWalkingSound();
        }
    }

    void StopWalkingSound()
    {
        if (movementSource.isPlaying) movementSource.Stop();
    }


    private System.Collections.IEnumerator PlayHapticForDuration(string clipName, float totalDuration, float clipLength)
    {
        float elapsed = 0f;

        while (elapsed < totalDuration)
        {
            BhapticsLibrary.Play(clipName);

            yield return new WaitForSeconds(clipLength);

            elapsed += clipLength;
        }

        BhapticsLibrary.StopByEventId(clipName);
    }

    private void LateUpdate()
    {
        UpdateLastPositions();

    }

    void WanderAwayFromPlayer()
    {
        agent.ResetPath();
        agent.isStopped = false;
        //agent.speed = 1.0f;

        Vector3 directionAway = (transform.position - player.position).normalized;

        Vector3 targetPosition = transform.position + (directionAway * 8f);

        targetPosition += Random.insideUnitSphere * 3f; // bit of randomness, so not walking directly away from player

        NavMeshHit hit;
        if (NavMesh.SamplePosition(targetPosition, out hit, 10f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
        else
        { // Fallback
            Wander();
        }
    }

    float CalculateCombinedMotion()
    {
        if (player == null || leftHand == null || rightHand == null) return 0;


        float headM = (player.position - lastPlayerPos).magnitude;
        float leftM = (leftHand.position - lastLeftPos).magnitude;
        float rightM = (rightHand.position - lastRightPos).magnitude;



        if (headM < deadzone) headM = 0;
        if (leftM < deadzone) leftM = 0;
        if (rightM < deadzone) rightM = 0;

        float totalMovement = (headM + leftM + rightM) / Time.deltaTime;
        //Debug.Log(totalMovement);
        //Debug.Log($"Total: {totalMovement:F2} | Head: {headM:F2} | L: {leftM:F2} | R: {rightM:F2}");

        // Sum of all movement 
        return totalMovement;
    }

    void UpdateLastPositions()
    {
        lastPlayerPos = player.position;
        lastLeftPos = leftHand.position;
        lastRightPos = rightHand.position;

        //Debug.Log(lastLeftPos);
        //Debug.Log(lastRightPos);
        //Debug.Log(lastPlayerPos);
    }

    void HandleHaptics(float distance, bool isHunting)
    {
        hapticTimer += Time.deltaTime;

        // Heartbeat  gets faster as monster gets closer
        float interval = Mathf.Clamp(distance / 10f, 0.4f, 1.0f);
        //Debug.Log(interval);
        //Debug.Log("Distance: " + distance);

        if (isHunting)
        {
            // Full Vest Rumble
            if (!BhapticsLibrary.IsPlayingByEventId(huntClip))
            {
                BhapticsLibrary.Play(huntClip);
                //BhapticsLibrary.Play(huntClip);
                //Debug.Log("Playing Hunting clip");
            }
            //if (!BhapticsLibrary.IsPlaying()) BhapticsLibrary.Play(huntClip);

            // Controller 

            leftHandHap.SendHapticImpulse(0, 0.8f, 0.1f);
            rightHandHap.SendHapticImpulse(0, 0.8f, 0.1f);

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

    bool CanSeePlayer()
    {
        Vector3 rayOrigin = transform.position + Vector3.up * eyeHeight;
        Vector3 directionToPlayer = (player.position - rayOrigin).normalized;
        if (Vector3.Angle(transform.forward, directionToPlayer) < viewAngle)
        {
            if (Physics.Raycast(rayOrigin, directionToPlayer, out RaycastHit hit, viewRange))
            {
                if (hit.transform != null)
                    //Debug.Log("Monster ray hit: " + hit.transform.name + " with tag: " + hit.transform.tag);


                return (hit.transform.root == player.root || hit.transform.CompareTag("Player") || hit.transform.CompareTag("MainCamera") || hit.transform == player || hit.transform == playerWheelchair);
            }
        }
        return false;
    }


    void Wander() 
    {

        agent.ResetPath();
        //agent.speed = 1.0f;

        for (int i = 0; i < 5; i++)
        {
            Vector3 randomPoint = transform.position + Random.insideUnitSphere * 10f;
            if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, 10f, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
                return;
            }
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