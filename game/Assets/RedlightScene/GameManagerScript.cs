using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

using OVR;

public class GameManagerScript : MonoBehaviour
{

    [Header("Game State")]
    public bool isGreenLight = true;
    public float timer = 0f;
    public bool isInsideGameBox = false;



    [Header("Detection Thresholds")]
    public float velocityThreshold = 0.15f; // head/hands
    public float angularVelocityThreshold = 1.0f; // head turning

    [Header("References")]
    public Renderer cubeRenderer;
    public Transform playerTransform; // The Capsule
    public Transform headTransform;   // CenterEyeAnchor
    public Transform spawnPoint;





    //private Vector3 lastPosition;
    //private Quaternion lastRotation;




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

        if (!isInsideGameBox) return;    // only if inside the box, start the game logic


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


    private Vector3 lastHeadPos;
    private Quaternion lastHeadRot;

    void DetectMovement()
    {
        // Controllers
        Vector3 leftVel = OVRInput.GetLocalControllerVelocity(OVRInput.Controller.LTouch);
        Vector3 rightVel = OVRInput.GetLocalControllerVelocity(OVRInput.Controller.RTouch);

        // Head
        Vector3 headLinearVel = (headTransform.position - lastHeadPos) / Time.deltaTime;
        float headAngularVel = Quaternion.Angle(headTransform.rotation, lastHeadRot) / Time.deltaTime;

        // Capsule Velocity (in case of joystick)
        CharacterController cc = playerTransform.GetComponent<CharacterController>();
        float bodySpeed = cc.velocity.magnitude;

        // Detection Logic
        if (leftVel.magnitude > velocityThreshold ||
            rightVel.magnitude > velocityThreshold ||
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
        // Moves player to spawn point and resets rotation
        playerTransform.position = spawnPoint.position;
        playerTransform.rotation = spawnPoint.rotation;

        //lastPosition = spawnPoint.position;
        //lastRotation = spawnPoint.rotation;

        // reset
        isGreenLight = true;
        timer = 3f;
        UpdateCubeColor();


    }
}

    

