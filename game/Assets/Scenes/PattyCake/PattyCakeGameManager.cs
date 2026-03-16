using UnityEngine;

public class PattyCakeGameManager : MonoBehaviour
{

    [Header("Game Elements")]
    public Renderer[] targetBoxes; 
    public AudioClip clapSound;
    private AudioSource audioSource;

    [Header("Colors")]
    public Material inactiveMaterial; // red
    public Material activeMaterial; // green

    private int currentActiveIndex = -1;


    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;

        // Start the game by lighting up the first box
        PickNextTarget();
    }

    public void PickNextTarget()
    { 
        foreach (Renderer box in targetBoxes)  // Reset boxes
        {
            box.material = inactiveMaterial;
        }

        // Pick random box for now, and set i active
        currentActiveIndex = Random.Range(0, targetBoxes.Length);
        targetBoxes[currentActiveIndex].material = activeMaterial;
    }


    // called by pattycake hands when collide with a box, returns true if it was the correct box
    public bool TryHit(GameObject hitBox)
    {
        GameObject expectedBox = targetBoxes[currentActiveIndex].gameObject;

        // This will print EXACTLY what you hit, and exactly what the manager EXPECTED you to hit
        Debug.Log($"Checking hit! I hit: [{hitBox.name}] ... I expected: [{expectedBox.name}]");

        if (currentActiveIndex >= 0 && hitBox == expectedBox)
        {
            audioSource.PlayOneShot(clapSound);
            Debug.Log("Clap");

            PickNextTarget(); // Pick a new box 
            return true;      
        }

        return false; // Wrong box hit
    }
}

