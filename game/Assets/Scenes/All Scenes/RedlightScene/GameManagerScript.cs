using TMPro;
using UnityEngine;

using System.Collections;

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
    private float velocityThreshold = 1.0f; // head/hands
    //public float velocityThreshold = 0.15f; // head/hands
    private float angularVelocityThreshold = 1.0f; // head turning

    [Header("References")]

    public Transform spawnPoint;


    [Header("Jumpscare References")]
    //public GameObject jumpscareUI;    // Canvas/Image here
    public AudioSource scareAudio;    // AudioSource here
    public float scareDuration = 1.0f;


    private Renderer cubeRenderer;
    private Transform playerTransform; // XR Origin
    private Transform headTransform;   // Main camera
    private Transform leftHandTransform;  // Left Controller 
    private Transform rightHandTransform; // Right Controller

    private CharacterController playerCC;


    private Vector3 lastHeadPos;
    private Quaternion lastHeadRot;
    private Vector3 lastLeftHandPos;
    private Vector3 lastRightHandPos;


    private bool Jumpscaring = false;




    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

        GameObject xrOrigin = GameObject.FindGameObjectWithTag("Player");
        GameObject mainCam = GameObject.FindGameObjectWithTag("MainCamera");
        GameObject leftHand = GameObject.FindGameObjectWithTag("Left Hand Box");
        GameObject rightHand = GameObject.FindGameObjectWithTag("Right Hand Box");



        playerTransform = xrOrigin.transform;
        playerCC = xrOrigin.GetComponent<CharacterController>(); 

        headTransform = mainCam.transform;
        leftHandTransform = leftHand.transform;
        rightHandTransform = rightHand.transform;

        UpdateTrackingData(); // Set initial positions 

    }

    // Update is called once per frame
    void Update()
    {

        if (isInsideGameBox)
        {    // only if inside the box, start the game logic


            timer -= Time.deltaTime;

            if (timer <= 0)
            {
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
            //if (Jumpscaring == false) StartCoroutine(JumpscareThenReset());
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

        playerCC.enabled = false;
        playerTransform.position = spawnPoint.position;
        playerTransform.rotation = spawnPoint.rotation;
        Physics.SyncTransforms();

        playerCC.enabled = true;



        // reset

        //isGreenLight = true;
        currentState = GameState.Green;
        timer = 3f;
        UpdateCubeColor();

        UpdateTrackingData();



    }
}

    

