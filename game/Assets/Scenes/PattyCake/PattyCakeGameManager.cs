using Unity.Hierarchy;
using UnityEngine;
using System.Collections;

public class PattyCakeGameManager : MonoBehaviour
{

    [Header("Game Elements")]
    public GameObject startBoxes; 
    public GameObject BBoxes;
    public GameObject girl;
    public GameObject monster;
    public GameObject spotlight;

    public Renderer[] targetBoxes; 
    public Renderer[] targetBoxesB4Baby;
    public AudioClip clapSound;
    public AudioClip pattyCakeSound;
    public AudioClip scream;
    public AudioClip youFailed;
    public AudioClip wellPlayed;
    public AudioClip letsPlay;

    private AudioSource audioSource;

    [Header("Colors")]
    public Material inactiveMaterial; // red
    public Material activeMaterial; // green

    private int currentActiveIndex = -1;
    private int gameState = 2; // 0 = left, 1 is clap, 2 is right, 3 is clap 

    private int gameCounter = 0; // before baby part

    private bool B4Baby = false; // 

    private float gameStartTime = 0f;
    private float gameAllowedTime = 13f;


    private bool isWaiting = false;



    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.PlayOneShot(letsPlay);

        // Start the game by lighting up the first box
        PickNextTarget();
    }

    public void PickNextTarget()
    {

        if (isWaiting) return;

        StartCoroutine(PickNextTargetRoutine());
    }

    private IEnumerator PickNextTargetRoutine()
    {
        isWaiting = true;

        if (!B4Baby)
        {

            foreach (Renderer box in targetBoxes)  // Reset boxes
            {
                box.material = inactiveMaterial;
            }

            // Pick random box for now, and set i active
            //currentActiveIndex = Random.Range(0, targetBoxes.Length);

            if (gameState == 2)
            {
                currentActiveIndex = 2;
                gameState = 3;


            } else if (gameState == 3)
            {
                currentActiveIndex = 0;
                gameState = 0;

            }
            else if (gameState == 0)
            {
                currentActiveIndex = 2;
                gameState = 1;
            }
            else if (gameState == 1)
            {
                currentActiveIndex = 1;
                gameState = 2;

            }

            targetBoxes[currentActiveIndex].material = activeMaterial;
            if (currentActiveIndex == 2) {
                targetBoxes[3].material = activeMaterial;
            }
        }

        gameCounter++;

        if (gameCounter == 2) // when game starts
        {
            audioSource.PlayOneShot(pattyCakeSound);
            Debug.Log("Patty Cake Sound Playing");
            gameStartTime = Time.time;
        }



        if (gameCounter == 21) // after 21 hits, switch to baby part
        //if (gameCounter == 21) // after 21 hits, switch to baby part
        {
            // disable old boxes
            startBoxes.SetActive(false);
            // enable new boxes
            BBoxes.SetActive(true);

            B4Baby = true;

            foreach (Renderer box in targetBoxesB4Baby)
            {
                box.material = inactiveMaterial;
            }

            currentActiveIndex = -1;
            //targetBoxesB4Baby[currentActiveIndex].material = activeMaterial;
        }



        //if (gameCounter > 21) // after 21 hits, switch to baby part
        if (B4Baby) // after 21 hits, switch to baby part
            {
                currentActiveIndex++;

                if (currentActiveIndex == targetBoxesB4Baby.Length)
                {
                //// disable all boxes
                startBoxes.SetActive(false);
                BBoxes.SetActive(false);
                //girl.SetActive(false);
                audioSource.PlayOneShot(wellPlayed);
                yield return new WaitForSeconds(wellPlayed.length);
                MySceneManager.Instance.UnloadOldScene("PattyCake-1");
                }

                targetBoxesB4Baby[currentActiveIndex].material = activeMaterial;

                

            }


        if (Time.time > gameStartTime + gameAllowedTime)   // if time runs out, you failed
        {
            


            // disable all boxes
            startBoxes.SetActive(false);
            BBoxes.SetActive(false);
            // play you failed sound
            audioSource.PlayOneShot(youFailed);
            yield return new WaitForSeconds(youFailed.length);

            // switch to monster
            girl.SetActive(false);
            monster.SetActive(true);

            // play scream
            audioSource.PlayOneShot(scream);
            //yield return new WaitForSeconds(scream.length);


            // flashing lights for 2 secs,
            
            int totalFlashes = 6;
            float waitTime = 0.14f;

            for (int i = 0; i < totalFlashes; i++)
            {
                spotlight.SetActive(true);
                yield return new WaitForSeconds(waitTime);

                spotlight.SetActive(false);

                yield return new WaitForSeconds(waitTime);
            }

            spotlight.SetActive(false);


            MySceneManager.Instance.UnloadOldScene("PattyCake-1");

        }


        isWaiting = false;

    }


    // called by pattycake hands when collide with a box, returns true if it was the correct box
    public bool TryHit(GameObject hitBox)
    {
        GameObject expectedBox;

        if (B4Baby) {
            expectedBox = targetBoxesB4Baby[currentActiveIndex].gameObject;
        }
        else
        {
            expectedBox = targetBoxes[currentActiveIndex].gameObject;

        }



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



