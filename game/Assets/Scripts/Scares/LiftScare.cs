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
    [SerializeField] private string walkStateName = "MonsterWalk";

    [Header("Approach Path")]
    [SerializeField] private Transform startPoint;
    [SerializeField] private Transform endPoint;
    [SerializeField] private float emergenceDelay = 1.0f;
    [SerializeField] private float approachDuration = 1.5f;

    [Header("Audio")]
    [SerializeField] private AudioSource footstepLoop;
    [SerializeField] private AudioSource hissLoop;
    [SerializeField] private float hissMinVolume = 0.1f;
    [SerializeField] private float hissMaxVolume = 1.0f;

    [Header("Lighting")]
    [SerializeField] private Light silhouetteLight;
    [SerializeField] private string firstAreaSceneName = "First Area";

    [Header("Timing")]
    [SerializeField] private float postFirstCloseDelay = 0.5f;
    [SerializeField] private float allIsWellDuration = 1.0f;

    private DoorMovement doors;
    private Transform playerHead;
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

        if (playerHead == null)
        {
            if (Camera.main != null)
                playerHead = Camera.main.transform;
        }

        if (monster == null || monsterAnimator == null || doors == null ||
            startPoint == null || endPoint == null)
        {
            Debug.LogWarning("[LiftScare] references not set");
            yield break;
        }

        Debug.LogWarning("[LiftScare] first close (normal)");
        yield return StartCoroutine(doors.closeDoorsRoutine());

        yield return new WaitForSeconds(postFirstCloseDelay);

        DisableFirstAreaLights();
        if (silhouetteLight != null) silhouetteLight.enabled = true;

        Debug.LogWarning("[LiftScare] doors reopening into darkness");
        yield return StartCoroutine(doors.openDoorsRoutine());

        Debug.LogWarning("[LiftScare] all is well pause");
        yield return new WaitForSeconds(allIsWellDuration);

        StartCoroutine(doors.closeDoorsRoutine());
        Debug.LogWarning("[LiftScare] second close + approach");

        yield return new WaitForSeconds(emergenceDelay);

        monster.transform.position = startPoint.position;
        monster.transform.rotation = startPoint.rotation;
        monster.SetActive(true);
        yield return null;

        monsterAnimator.Play(walkStateName, 0, 0f);
        if (footstepLoop != null) footstepLoop.Play();
        if (hissLoop != null)
        {
            hissLoop.volume = hissMinVolume;
            hissLoop.Play();
        }

        Quaternion startRot = startPoint.rotation;
        float elapsed = 0f;
        while (elapsed < approachDuration)
        {
            float t = elapsed / approachDuration;

            monster.transform.position = Vector3.Lerp(
                startPoint.position,
                endPoint.position,
                t
            );


            if (hissLoop != null)
            {
                hissLoop.volume = Mathf.Lerp(hissMinVolume, hissMaxVolume, t);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        monster.transform.position = endPoint.position;

        if (footstepLoop != null) footstepLoop.Stop();
        if (hissLoop != null) hissLoop.Stop();

        Debug.LogWarning("[LiftScare] monster reached lift, doors shut");

        RestoreFirstAreaLights();

        doors.StartCoroutine(doors.TransitionAndOpenDoors());
    }
}