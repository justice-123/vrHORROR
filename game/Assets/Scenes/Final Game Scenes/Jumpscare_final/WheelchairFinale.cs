using System.Collections;
using System.Collections.Generic;
using Bhaptics.SDK2;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;

public class WheelchairFinale : MonoBehaviour
{
    [Header("monster ")]
    [SerializeField] private GameObject monster;
    [SerializeField] private Animator monsterAnimator;
    [SerializeField] private NavMeshAgent agent;

    [Header("animator states")]
    [SerializeField] private string chaseStateName = "Chase";
    [SerializeField] private string attackStateName = "Attack";
    [SerializeField] private string idleStateName = "Idle";

    [Header("cross scene references")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private string spawnPointName = "DoorMarker";
    [SerializeField] private string horrorPostFXName = "HorrorPostFX";

    [Header("flickering lights")]
    [SerializeField] private Light[] flickerLights;
    [SerializeField] private float flickerDuration = 0.6f;

    [Header("audio effects")]
    [SerializeField] private AudioSource crashSound;
    [SerializeField] private AudioSource heartbeatAudio;
    [SerializeField] private AudioSource subBassRumble;
    [SerializeField] private AudioSource chaseAudio;
    [SerializeField] private AudioSource attackBoom;
    [SerializeField] private AudioSource attackScreech;
    [Tooltip(" distant ambient wind/city sounds for false safety phase.")]
    [SerializeField] private AudioSource ambientWind;
    [Tooltip(" tinnitus ringing after the attack.")]
    [SerializeField] private AudioSource tinnitusAudio;
    [SerializeField] private AudioSource beepAudio;


    [Header("timing of everything")]
    [SerializeField] private float falseSafetyDuration = 4f;
    [SerializeField] private float crashHoldDuration = 0.5f;
    [SerializeField] private float dreadBuildDuration = 4.0f;
    [SerializeField] private float fadeOutDuration = 0.4f;
    [SerializeField] private float blackHoldDuration = 0.4f;
    [SerializeField] private float fadeInDuration = 0.3f;
    [SerializeField] private float silenceBeforeAttack = 0.2f;

    [Header("haptic events")]
    [SerializeField] private string customHeartbeatEvent = "custom_heartbeat2";

    [Header("monster chase towards")]
    [SerializeField] private float attackTriggerDistance = 2.5f;
    [SerializeField] private float maxChaseTime = 5f;
    [SerializeField] private float chaseSpeed = 12f;
    [SerializeField] private float chaseAcceleration = 20f;

    [Header("death attack position")]
    [SerializeField] private float attackDistanceFromPlayer = 1.8f;
    [Tooltip("Vertical offset from camera Y. Press C to test. Try -0.3 to -0.7.")]
    [SerializeField] private float attackHeightOffset = -0.4f;

    [Header("post processing stuff")]
    [SerializeField] private float postFXRampDuration = 1f;

    [Header("night lighting ")]
    [SerializeField] private Color nightAmbientColor = new Color(0.06f, 0.08f, 0.12f);
    [SerializeField] private float nightAmbientIntensity = 0.25f;
    [SerializeField] private Light moonLight;
    [SerializeField] private Color moonLightColor = new Color(0.7f, 0.8f, 1f);
    [SerializeField] private float moonLightIntensity = 0.4f;
    [SerializeField] private string[] scenesToDarken = { "final_jumpscare", "Second Area" };

    [Header("fog")]
    [SerializeField] private bool useNightFog = true;
    [SerializeField] private Color nightFogColor = new Color(0.04f, 0.05f, 0.08f);
    [SerializeField] private float nightFogDensity = 0.05f;

  
    [SerializeField] private float holdOnBlackDuration = 5f;

    [Header("mild camera shake. keep values low")]
    [SerializeField] private float chaseShakeIntensity = 0.012f;
    [SerializeField] private float impactShakeIntensity = 0.08f;

    [Header("calibration")]
    [SerializeField] private bool enableCalibrationMode = true;

    // Runtime
    private Transform playerCamera, playerRig;
    private Transform spawnPoint;
    private ContinuousMoveProvider moveProvider;
    private ContinuousTurnProvider turnProvider;
    private SnapTurnProvider snapTurnProvider;
    private CanvasGroup fadeScreen;
    private Volume horrorPostFX;

    // State
    private bool hasTriggered = false;
    private bool isShakingCamera = false;
    private Vector3 rigShakeBasePos;
    private List<Light> disabledLights = new List<Light>();

    private void Awake()
    {
        if (monster != null) monster.SetActive(false);
        if (agent != null) agent.enabled = false;
    }

    private void Update()
    {
        if (!enableCalibrationMode) return;
        if (Input.GetKeyDown(KeyCode.C)) TestAttackPosition();
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("<color=yellow>we hit the collider!: </color>" + other.gameObject.name + " (Tag: " + other.tag + ")");

        if (hasTriggered) return;

        if (!other.CompareTag(playerTag))
        {
            Debug.Log("<color=red>Trigger ignored it because it wasn't tagged " + playerTag + "!</color>");
            return;
        }

        Debug.Log("<color=green> player has been detected, start the jumscare sequence now</color>");
        hasTriggered = true;
        StartCoroutine(PlayScare());
    }

    private void FindRuntimeReferences()
    {
        if (Camera.main != null) playerCamera = Camera.main.transform;

        GameObject p = GameObject.FindGameObjectWithTag(playerTag);
        if (p != null)
        {
            
            playerRig = p.transform.root;

            moveProvider = playerRig.GetComponentInChildren<ContinuousMoveProvider>();
            turnProvider = playerRig.GetComponentInChildren<ContinuousTurnProvider>();
            snapTurnProvider = playerRig.GetComponentInChildren<SnapTurnProvider>();
        }

        if (AreaTransition.Instance != null)
        {
            fadeScreen = AreaTransition.Instance.fadeScreen;
        }
        else
        {
            Debug.LogError("[FADE] AreaTransition.Instance is NULL - fade won't work!");
        }

        GameObject sp = GameObject.Find(spawnPointName);
        if (sp != null) spawnPoint = sp.transform;
        else Debug.LogError("[WheelchairFinale] Could not find: " + spawnPointName);

        GameObject postFX = GameObject.Find(horrorPostFXName);
        if (postFX != null) horrorPostFX = postFX.GetComponent<Volume>();
    }

    // main sequence now:
    private IEnumerator PlayScare()
    {
        FindRuntimeReferences();

        if (horrorPostFX != null) horrorPostFX.weight = 0f;

        if (monster == null || monsterAnimator == null || playerCamera == null ||
            playerRig == null || agent == null || spawnPoint == null)
        {
            Debug.LogError("[WheelchairFinale] Missing critical references, just break");
            yield break;
        }

        // player "thinks" they escaped 
        if (ambientWind != null)
        {
            ambientWind.volume = 0.4f;
            ambientWind.Play();
        }

        yield return new WaitForSeconds(falseSafetyDuration);

        // player realises smthg is wrong
        if (ambientWind != null) StartCoroutine(RampVolume(ambientWind, 0f, 0.8f));

        if (subBassRumble != null)
        {
            subBassRumble.volume = 0.05f;
            subBassRumble.Play();
            StartCoroutine(RampVolume(subBassRumble, 0.4f, 1.5f));
        }

        yield return new WaitForSeconds(0.8f);

        // slowly disable motion and trap the player wherever they are
        DisableControllers();

        if (crashSound != null) crashSound.Play();
        BhapticsLibrary.Play("jumpscare-back");
        StartCoroutine(FlickerLightsCinematic(flickerDuration));

        SpawnMonsterAtChosenPoint();
        monster.SetActive(true);
        monsterAnimator.applyRootMotion = false;
        monsterAnimator.Play(idleStateName, 0, 0f);

        StartCoroutine(RampPostFX(1f, postFXRampDuration));

        yield return new WaitForSeconds(crashHoldDuration);

        // building tension with the bhaptics custom heartbeat and composer audio

        if (heartbeatAudio != null)
        {
            heartbeatAudio.pitch = 1f;

            BhapticsLibrary.Play("custom_heartbeat2");
            heartbeatAudio.Play();
        }

        if (subBassRumble != null)
            StartCoroutine(RampVolume(subBassRumble, 0.9f, dreadBuildDuration));

        ApplyNightHorrorLighting();

        yield return new WaitForSeconds(dreadBuildDuration);

        if (subBassRumble != null)
            StartCoroutine(RampVolume(subBassRumble, 0.9f, dreadBuildDuration));

        ApplyNightHorrorLighting();

        yield return new WaitForSeconds(dreadBuildDuration);

        // fade to black then rotate the player to come face to face with the monster the make it chase at you
        yield return StartCoroutine(FadeAlpha(0f, 1f, fadeOutDuration));
        RotatePlayerToFaceSpawnPoint();
        yield return new WaitForSeconds(blackHoldDuration);

        
        monsterAnimator.Play(chaseStateName, 0, 0f);
        if (chaseAudio != null) chaseAudio.Play();

        rigShakeBasePos = playerRig.localPosition;
        isShakingCamera = true;
        StartCoroutine(CameraShakeLoop(chaseShakeIntensity));

        agent.enabled = true;
        yield return null;

        agent.isStopped = false;
        agent.speed = chaseSpeed;
        agent.acceleration = chaseAcceleration;
        agent.angularSpeed = 360f;
        agent.updateRotation = false;

        StartCoroutine(FadeAlpha(1f, 0f, fadeInDuration));

        // navmesh chase
        float timer = 0f;
        while (timer < maxChaseTime)
        {
            float distance = Vector2.Distance(
                new Vector2(monster.transform.position.x, monster.transform.position.z),
                new Vector2(playerCamera.position.x, playerCamera.position.z));

            if (distance <= attackTriggerDistance)
            {
                break;
            }

            Vector3 targetFloorPos = playerCamera.position;
            if (NavMesh.SamplePosition(playerCamera.position, out NavMeshHit playerHit, 5f, NavMesh.AllAreas))
            {
                targetFloorPos = playerHit.position;
            }
            agent.SetDestination(targetFloorPos);

            FaceMonsterAtPlayer();

            timer += Time.deltaTime;
            yield return null;
        }

        // do a quick fade to black and then "kill" the player
        yield return StartCoroutine(FadeAlpha(0f, 1f, 0.15f));

        agent.isStopped = true;
        agent.speed = 0;
        agent.velocity = Vector3.zero;
        agent.ResetPath();
        agent.enabled = false;

        // Ensure the custom heartbeat doesn't roll over into the attack
        BhapticsLibrary.StopByEventId(customHeartbeatEvent);

        if (chaseAudio != null && chaseAudio.isPlaying) chaseAudio.Stop();
        if (heartbeatAudio != null && heartbeatAudio.isPlaying) heartbeatAudio.Stop();
        if (subBassRumble != null && subBassRumble.isPlaying) subBassRumble.Stop();

        SnapMonsterToAttackPositionInFrontOfPlayer();
        monsterAnimator.Play(attackStateName, 0, 0f);
        monsterAnimator.Update(0f);

        yield return new WaitForSeconds(silenceBeforeAttack);

      
        if (fadeScreen != null) fadeScreen.alpha = 0f;

        if (attackBoom != null) attackBoom.Play();
        if (attackScreech != null) attackScreech.Play();
        StartCoroutine(ImpactShake(impactShakeIntensity, 0.2f));

        // Start the 3-second haptic event upon "killing"
        StartCoroutine(HuntVibrationLoop(3f));

        yield return new WaitForSeconds(0.4f);

        yield return StartCoroutine(FadeAlpha(0f, 1f, 0.15f));

        // hold the black- player dies womp womp
        isShakingCamera = false;

        if (monster != null) monster.SetActive(false);

        yield return new WaitForSeconds(1f);
        if (beepAudio != null)
        {
            GameObject beepObj = new GameObject("BeepSound");
            beepObj.transform.position = playerCamera.position;
            AudioSource src = beepObj.AddComponent<AudioSource>();
            src.clip = beepAudio.clip;
            src.volume = beepAudio.volume;
            src.spatialBlend = 0f;
            src.Play();
            Destroy(beepObj, beepAudio.clip.length + 0.1f);
        }

        if (tinnitusAudio != null)
        {
            tinnitusAudio.volume = 0.6f;
            tinnitusAudio.Play();
            //StartCoroutine(RampVolume(tinnitusAudio, 0f, holdOnBlackDuration));
        }

        yield return new WaitForSeconds(holdOnBlackDuration);

        if (fadeScreen != null) fadeScreen.alpha = 1f;

        OnFinaleComplete();
    }

    
    private IEnumerator HuntVibrationLoop(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            BhapticsLibrary.StopByEventId("hunt_vibration");
            BhapticsLibrary.Play("hunt_vibration");

            yield return new WaitForSecondsRealtime(0.2f);
            elapsed += 0.2f;
        }
    }

    //snap to attack position helper fn
    private void SnapMonsterToAttackPositionInFrontOfPlayer()
    {
        if (monster == null || playerCamera == null) return;

        Vector3 playerForward = playerCamera.forward;
        playerForward.y = 0;
        playerForward.Normalize();

        Vector3 targetPos = playerCamera.position + playerForward * attackDistanceFromPlayer;
        targetPos.y = playerCamera.position.y + attackHeightOffset;

        monster.transform.position = targetPos;
        FaceMonsterAtPlayer();
    }

    // make the monster spawn at a specific point helper fn
    private void SpawnMonsterAtChosenPoint()
    {
        if (spawnPoint == null) return;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(spawnPoint.position, out hit, 5f, NavMesh.AllAreas))
            monster.transform.position = hit.position;
        else
            monster.transform.position = spawnPoint.position;

        FaceMonsterAtPlayer();
    }

    // rotating the player to face exactly eye to eye with the monster helper fn
    private void RotatePlayerToFaceSpawnPoint()
    {
        if (playerRig == null || spawnPoint == null || playerCamera == null) return;

        Vector3 targetDir = spawnPoint.position - playerCamera.position;
        targetDir.y = 0;

        if (targetDir.sqrMagnitude < 0.001f) return;
        targetDir.Normalize();

        Vector3 currentCamDir = playerCamera.forward;
        currentCamDir.y = 0;
        currentCamDir.Normalize();

        float angleDifference = Vector3.SignedAngle(currentCamDir, targetDir, Vector3.up);

        playerRig.RotateAround(playerCamera.position, Vector3.up, angleDifference);
    }
    //face monster at player helper fn
    private void FaceMonsterAtPlayer()
    {
        if (monster == null || playerCamera == null) return;
        Vector3 lookPos = playerCamera.position;
        lookPos.y = monster.transform.position.y;
        monster.transform.LookAt(lookPos);
    }

   
    private void TestAttackPosition()
    {
        FindRuntimeReferences();
        if (monster == null || playerCamera == null) return;

        monster.SetActive(true);
        monsterAnimator.applyRootMotion = false;
        monsterAnimator.Play(attackStateName, 0, 0f);

        SnapMonsterToAttackPositionInFrontOfPlayer();

        Debug.Log("[CALIBRATE] Adjust attackHeightOffset and press C again.");
    }

    //fading helper fn
    private IEnumerator FadeAlpha(float startAlpha, float endAlpha, float duration)
    {
        if (fadeScreen == null) yield break;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            fadeScreen.alpha = Mathf.Lerp(startAlpha, endAlpha, elapsed / duration);
            yield return null;
        }
        fadeScreen.alpha = endAlpha;
    }

    //post processing helper fn
    private IEnumerator RampPostFX(float targetWeight, float duration)
    {
        if (horrorPostFX == null) yield break;
        float startWeight = horrorPostFX.weight;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            horrorPostFX.weight = Mathf.Lerp(startWeight, targetWeight, elapsed / duration);
            yield return null;
        }
        horrorPostFX.weight = targetWeight;
    }

    // night lighting effects
    private void ApplyNightHorrorLighting()
    {
        disabledLights.Clear();

        foreach (string sceneName in scenesToDarken)
        {
            Scene scene = SceneManager.GetSceneByName(sceneName);
            if (!scene.IsValid() || !scene.isLoaded) continue;

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Light[] lights = root.GetComponentsInChildren<Light>(true);
                foreach (Light l in lights)
                {
                    if (l == moonLight) continue;
                    if (!l.enabled) continue;

                    l.intensity *= 0.15f;
                }
            }
        }

        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = nightAmbientColor;
        RenderSettings.ambientIntensity = nightAmbientIntensity;

        if (moonLight != null)
        {
            moonLight.enabled = true;
            moonLight.color = moonLightColor;
            moonLight.intensity = moonLightIntensity;
            moonLight.type = LightType.Directional;
            moonLight.shadows = LightShadows.Soft;
            moonLight.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        if (useNightFog)
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = nightFogColor;
            RenderSettings.fogDensity = nightFogDensity;
        }
    }

    // flickering lights helper fn
    private IEnumerator FlickerLightsCinematic(float duration)
    {
        if (flickerLights == null || flickerLights.Length == 0) yield break;

        float[] originalIntensities = new float[flickerLights.Length];
        for (int i = 0; i < flickerLights.Length; i++)
            if (flickerLights[i] != null)
                originalIntensities[i] = flickerLights[i].intensity;

        SetLightsState(false, originalIntensities);
        yield return new WaitForSeconds(0.08f);

        SetLightsState(true, originalIntensities);
        yield return new WaitForSeconds(0.15f);

        SetLightsState(false, originalIntensities);
        yield return new WaitForSeconds(0.05f);

        for (int i = 0; i < flickerLights.Length; i++)
        {
            if (flickerLights[i] == null) continue;
            flickerLights[i].enabled = true;
            flickerLights[i].intensity = originalIntensities[i] * 0.4f;
        }
        yield return new WaitForSeconds(0.2f);

        float remaining = duration - 0.48f;
        if (remaining > 0)
        {
            float elapsed = 0f;
            while (elapsed < remaining)
            {
                for (int i = 0; i < flickerLights.Length; i++)
                {
                    if (flickerLights[i] == null) continue;
                    bool on = Random.value > 0.5f;
                    flickerLights[i].enabled = on;
                    if (on) flickerLights[i].intensity = originalIntensities[i] * Random.Range(0.2f, 0.7f);
                }
                float wait = Random.Range(0.04f, 0.1f);
                elapsed += wait;
                yield return new WaitForSeconds(wait);
            }
        }

        for (int i = 0; i < flickerLights.Length; i++)
            if (flickerLights[i] != null) flickerLights[i].enabled = false;
    }

    private void SetLightsState(bool on, float[] originalIntensities)
    {
        for (int i = 0; i < flickerLights.Length; i++)
        {
            if (flickerLights[i] == null) continue;
            flickerLights[i].enabled = on;
            if (on) flickerLights[i].intensity = originalIntensities[i];
        }
    }

   
    private IEnumerator CameraShakeLoop(float intensity)
    {
        while (isShakingCamera)
        {
            Vector3 shake = Random.insideUnitSphere * intensity;
            shake.y *= 0.3f;
            playerRig.localPosition = rigShakeBasePos + shake;
            yield return new WaitForSeconds(0.03f);
        }
        playerRig.localPosition = rigShakeBasePos;
    }

    private IEnumerator ImpactShake(float intensity, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float remaining = 1f - (elapsed / duration);
            Vector3 shake = Random.insideUnitSphere * intensity * remaining;
            shake.y *= 0.3f;
            playerRig.localPosition = rigShakeBasePos + shake;
            yield return null;
        }
        playerRig.localPosition = rigShakeBasePos;
    }

    //audio helper fn
    private IEnumerator RampVolume(AudioSource src, float targetVolume, float duration)
    {
        if (src == null) yield break;
        float startVol = src.volume;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            src.volume = Mathf.Lerp(startVol, targetVolume, elapsed / duration);
            yield return null;
        }
    }

    

    // helper fn to disable the controllers 
    private void DisableControllers()
    {
        if (moveProvider != null) moveProvider.enabled = false;
        if (turnProvider != null) turnProvider.enabled = false;
        if (snapTurnProvider != null) snapTurnProvider.enabled = false;
    }

    private void OnFinaleComplete()
    {
        Debug.Log("[WheelchairFinale] complete");
    }
}