using TMPro;
using UnityEngine;
//using UnityEngine.InputSystem;
//using UnityEngine.SceneManagement;
//using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

public class GameManagerScript : MonoBehaviour
{

    public enum GameState { Green, Orange, Red }

    [Header("Game State")]
    //public bool isGreenLight = true;
    public GameState currentState = GameState.Green;
    public float timer = 0f;
    public bool isInsideGameBox = false;
    public float orangeDuration = 1.5f;



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
                //isGreenLight = !isGreenLight;
                //timer = Random.Range(2f, 5f);
                //UpdateCubeColor();
                CycleLight();
            }


            if (currentState == GameState.Red)
            {
                DetectMovement();
            }

        }


        UpdateTrackingData();



    }

    void CycleLight()
    {
        if (currentState == GameState.Green)
        {
            currentState = GameState.Orange;
            timer = orangeDuration; 
        }
        else if (currentState == GameState.Orange)
        {
            currentState = GameState.Red;
            timer = Random.Range(2f, 5f); 
        }
        else
        {
            currentState = GameState.Green;
            timer = Random.Range(2f, 5f); 
        }
        UpdateCubeColor();
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
            //isGreenLight = true; // When player leaves box, rest to green
            currentState = GameState.Green;
            UpdateCubeColor();
        }
    }


    void UpdateCubeColor()
    {
        switch (currentState)
        {
            case GameState.Green:
                cubeRenderer.material.color = Color.green;
                break;
            case GameState.Orange:
                cubeRenderer.material.color = new Color(1f, 0.5f, 0f);
                break;
            case GameState.Red:
                cubeRenderer.material.color = Color.red;
                break;
        }
    }


    void UpdateTrackingData()
    {
        lastHeadPos = headTransform.position;
        lastHeadRot = headTransform.rotation;
        lastLeftHandPos = leftHandTransform.position;
        lastRightHandPos = rightHandTransform.position;
    }


    public void ResetPlayer()
    {
        

        playerTransform.position = spawnPoint.position;
        playerTransform.rotation = spawnPoint.rotation;
        Physics.SyncTransforms();


        // reset

        //isGreenLight = true;
        currentState = GameState.Green;
        timer = 3f;
        UpdateCubeColor();


        UpdateTrackingData();

        //SceneManager.LoadScene(SceneManager.GetActiveScene().name);


    }
}

    

