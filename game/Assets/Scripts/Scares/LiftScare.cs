using System.Collections;
using UnityEngine;

public class LiftScare : MonoBehaviour
{
    public static LiftScare Instance { get; private set; }

    [Header("Monster")]
    [SerializeField] private GameObject monster;
    [SerializeField] private Animator monsterAnimator;
    [SerializeField] private string idleStateName = "Idle";
    [SerializeField] private string attackStateName = "Attack";

    [Header("Audio")]
    [SerializeField] private AudioSource peekAudio;
    [SerializeField] private AudioSource screechAudio;

    [Header("Timing")]
    [SerializeField] private float silenceBeforeScare = 1.5f;
    [SerializeField] private float idleDuration = 1.5f;
    [SerializeField] private float attackTailDuration = 0.5f;

    private DoorMovement doors;

    private void Awake()
    {
        Debug.LogWarning("LiftScare Awake running on " + gameObject.name);

        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (monster != null)
            monster.SetActive(false);
    }

    public IEnumerator PlayScare()
    {
        Debug.LogWarning("LiftScare PlayScare starting");

        if (doors == null)
        {
            GameObject lift = GameObject.FindWithTag("Lift");
            if (lift != null)
                doors = lift.GetComponentInChildren<DoorMovement>();
        }

        if (monster == null || monsterAnimator == null || doors == null)
        {
            Debug.LogWarning("LiftScare references not set");
            yield break;
        }

        Debug.LogWarning("LiftScare closing doors");
        yield return StartCoroutine(doors.closeDoorsRoutine());

        yield return new WaitForSeconds(silenceBeforeScare);

        monster.SetActive(true);
        yield return null;
        monsterAnimator.Play(idleStateName, 0, 0f);
        if (peekAudio != null) peekAudio.Play();

        doors.crackDoorsOpen();
        Debug.LogWarning("LiftScare doors cracking open, idle phase");

        yield return new WaitForSeconds(idleDuration);

        if (peekAudio != null && peekAudio.isPlaying) peekAudio.Stop();
        monsterAnimator.Play(attackStateName, 0, 0f);
        if (screechAudio != null) screechAudio.Play();

        yield return StartCoroutine(doors.closeDoorsRoutine());
        Debug.LogWarning("LiftScare attack phase, doors slammed");

        yield return new WaitForSeconds(attackTailDuration);

        yield return StartCoroutine(AreaTransition.Instance.StartAreaTransition());
        doors.openDoors();
    }
}