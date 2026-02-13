using UnityEngine;

public class GameManagerScript : MonoBehaviour
{

    public bool isGreenLight = true;
    public float timer = 0f;

    public Renderer cubeRenderer;


    [Header("Detection Settings")]
    public Transform playerTransform;
    public float moveThreshold = 0.01f; // Tiny movements allowed
    public float turnThreshold = 0.05f;

    private Vector3 lastPosition;
    private Quaternion lastRotation;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

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
            }

            lastPosition = playerTransform.position;
            lastRotation = playerTransform.rotation;
        }

    }

