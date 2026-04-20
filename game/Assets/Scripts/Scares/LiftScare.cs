using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

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

    [Header("Lighting")]
    [SerializeField] private Light silhouetteLight;
    [SerializeField] private string firstAreaSceneName = "First Area";

    [Header("Timing")]
    [SerializeField] private float silenceBeforeScare = 1.5f;
    [SerializeField] private float idleDuration = 0.4f;
    [SerializeField] private float attackTailDuration = 0.5f;

    private DoorMovement doors;
    private List<Light> disabledLights = new List<Light>();

    private Color cachedAmbientLight;
    private AmbientMode cachedAmbientMode;
    private float cachedAmbientIntensity;
    private float cachedReflectionIntensity;

    private void Awake()
    {
        Debug.LogWarning("[LiftScare] Awake running on " + gameObject.name);

        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (monster != null)
            monster.SetActive(false);
    }

    private void DisableFirstAreaLights()
    {
        disabledLights.Clear();

        Scene firstArea = SceneManager.GetSceneByName(firstAreaSceneName);
        if (!firstArea.IsValid() || !firstArea.isLoaded)
        {
            Debug.LogWarning("[LiftScare] First Area scene not found or not loaded");
            return;
        }

        foreach (GameObject root in firstArea.GetRootGameObjects())
        {
            Light[] lights = root.GetComponentsInChildren<Light>(true);
            foreach (Light l in lights)
            {
                if (l == silhouetteLight) continue;
                if (!l.enabled) continue;

                l.enabled = false;
                disabledLights.Add(l);
            }
        }

        cachedAmbientMode = RenderSettings.ambientMode;
        cachedAmbientLight = RenderSettings.ambientLight;
        cachedAmbientIntensity = RenderSettings.ambientIntensity;
        cachedReflectionIntensity = RenderSettings.reflectionIntensity;

        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = Color.black;
        RenderSettings.ambientIntensity = 0f;
        RenderSettings.reflectionIntensity = 0f;

        Debug.LogWarning("[LiftScare] disabled " + disabledLights.Count + " lights + ambient");
    }

    private void RestoreFirstAreaLights()
    {
        foreach (Light l in disabledLights)
        {
            if (l != null) l.enabled = true;
        }
        disabledLights.Clear();

        RenderSettings.ambientMode = cachedAmbientMode;
        RenderSettings.ambientLight = cachedAmbientLight;
        RenderSettings.ambientIntensity = cachedAmbientIntensity;
        RenderSettings.reflectionIntensity = cachedReflectionIntensity;
    }

    public IEnumerator PlayScare()
    {
        Debug.LogWarning("[LiftScare] PlayScare starting");

        if (doors == null)
        {
            GameObject lift = GameObject.FindWithTag("Lift");
            if (lift != null)
                doors = lift.GetComponentInChildren<DoorMovement>();
        }

        if (monster == null || monsterAnimator == null || doors == null)
        {
            Debug.LogWarning("[LiftScare] references not set");
            yield break;
        }

        Debug.LogWarning("[LiftScare] closing doors");
        yield return StartCoroutine(doors.closeDoorsRoutine());

        yield return new WaitForSeconds(silenceBeforeScare);

        DisableFirstAreaLights();
        if (silhouetteLight != null) silhouetteLight.enabled = true;

        monster.SetActive(true);
        yield return null;
        monsterAnimator.Play(idleStateName, 0, 0f);
        if (peekAudio != null) peekAudio.Play();

        doors.crackDoorsOpen();
        Debug.LogWarning("[LiftScare] doors cracking open, idle phase");

        yield return new WaitForSeconds(idleDuration);

        if (peekAudio != null && peekAudio.isPlaying) peekAudio.Stop();
        monsterAnimator.Play(attackStateName, 0, 0f);
        if (screechAudio != null) screechAudio.Play();

        StartCoroutine(doors.closeDoorsRoutine());
        Debug.LogWarning("[LiftScare] attack + doors closing simultaneously");

        yield return new WaitForSeconds(attackTailDuration);
        RestoreFirstAreaLights();
        doors.StartCoroutine(doors.TransitionAndOpenDoors());
    }
}