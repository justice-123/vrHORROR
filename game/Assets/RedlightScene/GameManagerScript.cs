using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
public class GameManagerScript : MonoBehaviour
{

    public bool isGreenLight = true;
    public float timer = 0f;

    public Renderer cubeRenderer;


    [Header("Detection Settings")]
    public Transform playerTransform;
    public float moveThreshold = 1.05f; // Tiny movements allowed
    public float turnThreshold = 1.1f;

    public Transform spawnPoint;

    private Vector3 lastPosition;
    private Quaternion lastRotation;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ResetPlayer();

        lastPosition = playerTransform.position;
        lastRotation = playerTransform.rotation;

        UpdateCubeColor();
    }

    // Update is called once per frame
    void Update()
    {
        timer -= Time.deltaTime;

        if (timer <= 0)
        {
            isGreenLight = !isGreenLight;
            timer = Random.Range(2f, 5f);
            UpdateCubeColor();


            if (isGreenLight)
            {
                lastPosition = playerTransform.position;
                lastRotation = playerTransform.rotation;

            }
            if (!isGreenLight)
            {
                lastPosition = playerTransform.position;
                lastRotation = playerTransform.rotation;
            }
        }
           

            

        if (!isGreenLight)
        {
            DetectMovement();
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

        void DetectMovement()
        {

            float moveDistance = Vector3.Distance(playerTransform.position, lastPosition);
            float turnAngle = Quaternion.Angle(playerTransform.rotation, lastRotation);

        if (moveDistance > moveThreshold || turnAngle > turnThreshold)
        {
            Debug.Log("DEAD motion detected.");
            ResetPlayer();
        }

            lastPosition = playerTransform.position;
            lastRotation = playerTransform.rotation;
        }


    public void ResetPlayer()
    {
        // Moves player to spawn point and resets rotation
        playerTransform.position = spawnPoint.position;
        playerTransform.rotation = spawnPoint.rotation;

        lastPosition = spawnPoint.position;
        lastRotation = spawnPoint.rotation;

        // reset
        isGreenLight = true;
        timer = 3f;
        UpdateCubeColor();


    }
}

    

