using System.Collections;
using UnityEngine;

public class WindowJumpscare : MonoBehaviour
{
    [Header("Gurney Settings")]
    [SerializeField] AudioSource cartwheelscreech;
    [SerializeField] GameObject Surgical_Table;

    [Header("Monster Settings")]
    [SerializeField] GameObject girl; // Drag your monster here
    [SerializeField] Light scareLight;   // Drag your point light here
    [SerializeField] float delayBeforeMonster = 3.0f; // Seconds the gurney rolls before the scare

    void OnTriggerEnter(Collider other)
    {
        // Only trigger for the player
        if (other.CompareTag("Player") || other.CompareTag("MainCamera"))
        {
            // 1. Start the Gurney & Sound immediately
            Surgical_Table.GetComponent<Animator>().Play("windowScare_prequel");
            cartwheelscreech.Play();

            // 2. Start the timer for the monster
            StartCoroutine(MonsterRevealRoutine());

            // 3. Disable the trigger so it only happens once
            this.GetComponent<BoxCollider>().enabled = false;
        }
    }

    IEnumerator MonsterRevealRoutine()
    {
        // Wait for the gurney to finish or reach a certain point
        yield return new WaitForSeconds(delayBeforeMonster);

        // 3. Make the monster appear and the light flash
        girl.SetActive(true);
        scareLight.intensity = 20f; // High intensity for a "flash"

        // Optional: Play a scream sound here if you have one
        // monster.GetComponent<AudioSource>().Play();

        yield return new WaitForSeconds(0.2f);
        scareLight.intensity = 2f; // Dim the light so the player can see the monster
    }
}