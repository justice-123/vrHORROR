using TMPro;
using UnityEngine;
//using UnityEngine.InputSystem;
//using UnityEngine.SceneManagement;
//using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

public class GameManagerScript : MonoBehaviour
{

    [Header("Game State")]
    public bool isGreenLight = true;
    public float timer = 0f;
    public bool isInsideGameBox = false;



    [Header("Detection Thresholds")]
    public float velocityThreshold = 1f; // head/hands
    //public float velocityThreshold = 0.15f; // head/hands
    public float angularVelocityThreshold = 2.0f; // head turning

    [Header("References")]
    public Renderer cubeRenderer;
    public Transform playerTransform; // XR Origin
    public Transform headTransform;   // Main camera
    public Transform leftHandTransform;  // Left Controller 
    public Transform rightHandTransform; // Right Controller
    public Transform spawnPoint;


    //[Header("Controllers")]
    //public InputActionReference leftHandVelocity;  
    //public InputActionReference rightHandVelocity;


    //public TeleportationProvider teleportationProvider;





    private Vector3 lastHeadPos;
    private Quaternion lastHeadRot;
    private Vector3 lastLeftHandPos;
    private Vector3 lastRightHandPos;




    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ResetPlayer();

        //lastPosition = playerTransform.position;
        //lastRotation = playerTransform.rotation;

        //UpdateCubeColor();

    }

    // Update is called once per frame
    void Update()
    {

        if (isInsideGameBox)
        {    // only if inside the box, start the game logic


            timer -= Time.deltaTime;

            if (timer <= 0)
            {
                isGreenLight = !isGreenLight;
                timer = Random.Range(2f, 5f);
                UpdateCubeColor();
            }


            if (!isGreenLight)
            {
                DetectMovement();
            }

        }
        lastHeadPos = headTransform.position;
        lastHeadRot = headTransform.rotation;
        lastLeftHandPos = leftHandTransform.position;
        lastRightHandPos = rightHandTransform.position;


    }


    void DetectMovement()
    {
        // Controllers
        // 1. Hand Velocities (Calculated by position differences)
        //float leftHandSpeed = 0f;
        //float rightHandSpeed = 0f;

        float leftHandSpeed = ((leftHandTransform.position - lastLeftHandPos) / Time.deltaTime).magnitude;

        float rightHandSpeed = ((rightHandTransform.position - lastRightHandPos) / Time.deltaTime).magnitude;

        // Head
        Vector3 headLinearVel = (headTransform.position - lastHeadPos) / Time.deltaTime;
        float headAngularVel = Quaternion.Angle(headTransform.rotation, lastHeadRot) / Time.deltaTime;


        // Capsule Velocity (in case of joystick)
        CharacterController cc = playerTransform.GetComponent<CharacterController>();
        float bodySpeed = cc.velocity.magnitude;

        // Detection Logic
        if (leftHandSpeed > velocityThreshold ||
            rightHandSpeed > velocityThreshold ||
            headLinearVel.magnitude > velocityThreshold ||
            headAngularVel > angularVelocityThreshold ||
            bodySpeed > 0.1f)
        {
            Debug.Log("ELIMINATED: Motion detected!");
            ResetPlayer();
        }
    }

    public void SetPlayerInZone(bool inside)
    {
        isInsideGameBox = inside;
        if (!inside)
        {
            isGreenLight = true; // When player leaves box, rest to green
            UpdateCubeColor();
        }
    }


    void UpdateCubeColor()
        {
            if (isGreenLight)
            {
                cubeRenderer.material.color = Color.green;
            }
            else
            {
                cubeRenderer.material.color = Color.red;
            }


        }


    public void ResetPlayer()
    {
        // Moves player to spawn point and resets rotation, disables controllers for teleport

        //CharacterController cc = playerTransform.GetComponent<CharacterController>();
        //Rigidbody rb = playerTransform.GetComponentInChildren<Rigidbody>();

        //if (cc != null) cc.enabled = false;
        //if (rb != null) rb.isKinematic = true;




        //TeleportRequest request = new TeleportRequest
        //{
        //    destinationPosition = spawnPoint.position,
        //    destinationRotation = spawnPoint.rotation,
        //    matchOrientation = MatchOrientation.WorldSpaceUp
        //};

        //// Execute the teleport
        //teleportationProvider.QueueTeleportRequest(request);



        playerTransform.position = spawnPoint.position;
        playerTransform.rotation = spawnPoint.rotation;
        Physics.SyncTransforms();

        //if (cc != null) cc.enabled = true;
        //if (rb != null) rb.isKinematic = false;

        //lastPosition = spawnPoint.position;
        //lastRotation = spawnPoint.rotation;

        // reset
        isGreenLight = true;
        timer = 3f;
        UpdateCubeColor();


        lastHeadPos = headTransform.position;
        lastHeadRot = headTransform.rotation;

        //SceneManager.LoadScene(SceneManager.GetActiveScene().name);


    }
}

    

