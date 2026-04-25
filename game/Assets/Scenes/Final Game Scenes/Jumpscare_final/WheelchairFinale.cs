using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;
using UnityEngine.AI; // <--- NEW: Required for NavMesh

public class WheelchairFinale : MonoBehaviour
{
    [Header("=== MONSTER ===")]
    [SerializeField] private GameObject monster;
    [SerializeField] private Animator monsterAnimator;
    [SerializeField] private NavMeshAgent agent; // <--- NEW: Assign the Agent here!

    [Header("=== ANIMATOR STATES ===")]
    [SerializeField] private string chaseStateName = "Chase";
    [SerializeField] private string attackStateName = "Attack";

    [Header("=== CROSS-SCENE REFERENCES ===")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private string redVignetteName = "RedVignette";
    [SerializeField] private string fadeImageName = "FadeImage";

    [Header("=== FLICKER LIGHTS ===")]
    [SerializeField] private Light[] flickerLights;
    [SerializeField] private float flickerDuration = 0.5f;

    [Header("=== AUDIO ===")]
    [SerializeField] private AudioSource crashSound;
    [SerializeField] private AudioSource heartbeatAudio;
    [SerializeField] private AudioSource subBassRumble;
    [SerializeField] private AudioSource chaseAudio;
    [SerializeField] private AudioSource attackBoom;
    [SerializeField] private AudioSource attackScreech;

    [Header("=== TIMING & TRIGGERS ===")]
    [SerializeField] private float crashHoldDuration = 0.5f;
    [SerializeField] private float dreadBuildDuration = 2.5f;
    [SerializeField] private float silenceBeforeAttack = 0.15f;

    [SerializeField] private float gazeTriggerThreshold = 0.6f;
    [SerializeField] private float maxWaitToLookTime = 10f;

    [Header("=== MONSTER SPAWN (OUTSIDE PLAYER VIEW) ===")]
    [SerializeField] private float spawnDistance = 8f;
    [SerializeField] private float minSpawnAngle = 100f;
    [SerializeField] private float maxSpawnAngle = 180f;

    [Header("=== CHASE SPEED ===")]
    [SerializeField] private float attackDistance = 2.8f;
    [SerializeField] private float maxChaseTime = 3f;

    [Header("=== ATTACK POSITION (seated) ===")]
    [SerializeField] private bool useManualHeight = true;
    [SerializeField] private float manualHeightOverride = -0.4f;
    [SerializeField] private float attackStopDistance = 1.8f;

    [Header("=== RED LIGHTING ===")]
    [SerializeField] private Color redAmbientColor = new Color(0.5f, 0.05f, 0.05f);
    [SerializeField] private float redAmbientIntensity = 0.6f;
    [SerializeField] private string[] scenesToDarken = { "final_jumpscare", "Second Area" };

    [Header("=== IMPACT ===")]
    [SerializeField] private float redFlashDuration = 0.3f;
    [SerializeField] private float holdOnBlackDuration = 4f;

    [Header("=== CAMERA SHAKE ===")]
    [SerializeField] private float chaseShakeIntensity = 0.015f;
    [SerializeField] private float impactShakeIntensity = 0.08f;

    // Runtime
    private Transform playerCamera, playerRig;
    private ContinuousMoveProvider moveProvider;
    private ContinuousTurnProvider turnProvider;
    private SnapTurnProvider snapTurnProvider;
    private Image redVignetteImage;
    private Image fadeImage;

    // State
    private bool hasTriggered = false;
    private bool isShakingCamera = false;
    private Vector3 rigShakeBasePos;
    private List<Light> disabledLights = new List<Light>();

    private void Awake()
    {
        if (monster != null) monster.SetActive(false);
        if (agent != null) agent.enabled = false; // Keep agent off until needed
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered) return;
        if (!other.CompareTag(playerTag)) return;
        hasTriggered = true;
        StartCoroutine(PlayScare());
    }

    private void FindRuntimeReferences()
    {
        if (Camera.main != null) playerCamera = Camera.main.transform;

        GameObject p = GameObject.FindGameObjectWithTag(playerTag);
        if (p != null)
        {
            playerRig = p.transform;
            moveProvider = p.GetComponentInChildren<ContinuousMoveProvider>();
            turnProvider = p.GetComponentInChildren<ContinuousTurnProvider>();
            snapTurnProvider = p.GetComponentInChildren<SnapTurnProvider>();
        }

        GameObject vignette = GameObject.Find(redVignetteName);
        if (vignette != null) redVignetteImage = vignette.GetComponent<Image>();

        GameObject fade = GameObject.Find(fadeImageName);
        if (fade != null) fadeImage = fade.GetComponent<Image>();
    }

    private IEnumerator PlayScare()
    {
        FindRuntimeReferences();
        EnsureFadeImageExists();

        if (redVignetteImage != null) redVignetteImage.color = new Color(0.9f, 0, 0, 0);

        if (monster == null || monsterAnimator == null || playerCamera == null || agent == null)
        {
            Debug.LogError("[WheelchairFinale] missing critical refs (Did you assign the NavMeshAgent?) - aborting");
            yield break;
        }

        // === STEP 1: TRAP ===
        DisableControllers();
        if (crashSound != null) crashSound.Play();
        StartCoroutine(FlickerLightsOnce(flickerDuration));

        SpawnMonsterOutsidePlayerView();
        monster.SetActive(true);
        monsterAnimator.applyRootMotion = false;
        monsterAnimator.Play("Idle", 0, 0f);

        yield return new WaitForSeconds(crashHoldDuration);

        // === STEP 2: HEARTBEAT + DREAD BUILDS ===
        if (heartbeatAudio != null)
        {
            heartbeatAudio.pitch = 1f;
            heartbeatAudio.Play();
            StartCoroutine(RampPitch(heartbeatAudio, 1.6f, dreadBuildDuration + maxWaitToLookTime));
        }

        if (subBassRumble != null)
        {
            subBassRumble.volume = 0.2f;
            subBassRumble.Play();
            StartCoroutine(RampVolume(subBassRumble, 0.9f, dreadBuildDuration));
        }

        ApplyRedHorrorLighting();
        StartCoroutine(PulseRedVignette());

        yield return new WaitForSeconds(dreadBuildDuration);

        // === STEP 2.5: WAIT FOR PLAYER TO LOOK ===
        float waitTimer = 0f;
        while (waitTimer < maxWaitToLookTime)
        {
            Vector3 dirToMonster = (monster.transform.position - playerCamera.position).normalized;
            dirToMonster.y = 0;
            Vector3 flatCamForward = playerCamera.forward;
            flatCamForward.y = 0;

            float dotProduct = Vector3.Dot(flatCamForward.normalized, dirToMonster.normalized);

            if (dotProduct >= gazeTriggerThreshold) break;

            waitTimer += Time.deltaTime;
            yield return null;
        }

        // === STEP 3: CHASE BEGINS ===
        monsterAnimator.Play(chaseStateName, 0, 0f);
        if (chaseAudio != null) chaseAudio.Play();

        rigShakeBasePos = playerRig.localPosition;
        isShakingCamera = true;
        StartCoroutine(CameraShakeLoop(chaseShakeIntensity));

        // === STEP 4: NAVMESH CHASE ===
        float timer = 0f;
        agent.enabled = true; // Turn the agent on!

        while (timer < maxChaseTime)
        {
            // The NavMesh handles the pathing and rotation automatically now
            agent.SetDestination(playerCamera.position);

            float horizontalDist = Vector2.Distance(
                new Vector2(monster.transform.position.x, monster.transform.position.z),
                new Vector2(playerCamera.position.x, playerCamera.position.z));

            if (horizontalDist <= attackDistance) break;

            timer += Time.deltaTime;
            yield return null;
        }

        // Stop the agent completely
        agent.isStopped = true;
        agent.velocity = Vector3.zero;

        // === STEP 5: POSITION FOR ATTACK ===
        PositionMonsterForAttack();

        if (chaseAudio != null && chaseAudio.isPlaying) chaseAudio.Stop();
        if (heartbeatAudio != null && heartbeatAudio.isPlaying) heartbeatAudio.Stop();
        if (subBassRumble != null && subBassRumble.isPlaying) subBassRumble.Stop();

        // === STEP 6: SILENT BEAT ===
        yield return new WaitForSeconds(silenceBeforeAttack);

        // === STEP 7: ATTACK + IMPACT ===
        monsterAnimator.Play(attackStateName, 0, 0f);
        if (attackBoom != null) attackBoom.Play();
        if (attackScreech != null) attackScreech.Play();

        StartCoroutine(ImpactShake(impactShakeIntensity, redFlashDuration));

        // Turn agent OFF for the final jump so we can forcefully Lerp into the camera
        agent.enabled = false;
        yield return StartCoroutine(RedFlashAndFillVision());

        // === STEP 8: HOLD ON BLACK ===
        isShakingCamera = false;
        yield return new WaitForSeconds(holdOnBlackDuration);

        OnFinaleComplete();
    }

    private void SpawnMonsterOutsidePlayerView()
    {
        Vector3 playerForward = playerCamera.forward;
        playerForward.y = 0;
        playerForward.Normalize();

        float randomAngle = Random.Range(minSpawnAngle, maxSpawnAngle);
        if (Random.value < 0.5f) randomAngle = -randomAngle;

        Quaternion rotation = Quaternion.Euler(0, randomAngle, 0);
        Vector3 spawnDir = rotation * playerForward;
        Vector3 desiredSpawnPos = playerCamera.position + spawnDir * spawnDistance;

        // Use NavMesh to find the closest valid floor point near our desired spawn
        NavMeshHit hit;
        if (NavMesh.SamplePosition(desiredSpawnPos, out hit, 5f, NavMesh.AllAreas))
        {
            monster.transform.position = hit.position;
        }
        else
        {
            Debug.LogWarning("[WheelchairFinale] Could not find NavMesh point for spawn. Falling back.");
            desiredSpawnPos.y = playerCamera.position.y - 1.5f;
            monster.transform.position = desiredSpawnPos;
        }

        FaceMonsterAtPlayer();
    }

    private void PositionMonsterForAttack()
    {
        Vector3 dirToPlayer = (playerCamera.position - monster.transform.position);
        dirToPlayer.y = 0;
        dirToPlayer.Normalize();

        Vector3 targetPos = playerCamera.position - dirToPlayer * attackStopDistance;

        // Ensure the attack position is still valid on the NavMesh
        NavMeshHit hit;
        if (NavMesh.SamplePosition(targetPos, out hit, 2f, NavMesh.AllAreas))
        {
            monster.transform.position = hit.position;
        }
        else
        {
            monster.transform.position = targetPos;
        }

        FaceMonsterAtPlayer();

        if (useManualHeight)
        {
            Vector3 adjusted = monster.transform.position;
            adjusted.y += manualHeightOverride;
            monster.transform.position = adjusted;
        }
    }

    private void FaceMonsterAtPlayer()
    {
        if (monster == null || playerCamera == null) return;
        Vector3 lookPos = playerCamera.position;
        lookPos.y = monster.transform.position.y;
        monster.transform.LookAt(lookPos);
    }

    // --- (The rest of your helper coroutines remain unchanged below) ---

    private IEnumerator FlickerLightsOnce(float duration)
    {
        if (flickerLights == null || flickerLights.Length == 0) yield break;
        bool[] originalStates = new bool[flickerLights.Length];
        for (int i = 0; i < flickerLights.Length; i++)
            if (flickerLights[i] != null) originalStates[i] = flickerLights[i].enabled;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            foreach (Light l in flickerLights)
                if (l != null) l.enabled = Random.value > 0.5f;
            float wait = Random.Range(0.04f, 0.12f);
            elapsed += wait;
            yield return new WaitForSeconds(wait);
        }

        for (int i = 0; i < flickerLights.Length; i++)
            if (flickerLights[i] != null) flickerLights[i].enabled = originalStates[i];
    }

    private void ApplyRedHorrorLighting()
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
                    if (!l.enabled) continue;
                    l.enabled = false;
                    disabledLights.Add(l);
                }
            }
        }
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = redAmbientColor;
        RenderSettings.ambientIntensity = redAmbientIntensity;
    }

    private IEnumerator PulseRedVignette()
    {
        if (redVignetteImage == null) yield break;
        Color off = new Color(0.9f, 0f, 0f, 0f);
        Color on = new Color(0.9f, 0f, 0f, 0.5f);
        float t = 0f;
        while (isShakingCamera || t < 5f)
        {
            t += Time.deltaTime;
            float pulse = (Mathf.Sin(t * 5f) + 1f) * 0.5f;
            redVignetteImage.color = Color.Lerp(off, on, pulse);
            yield return null;
        }
        redVignetteImage.color = off;
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

    private IEnumerator RampPitch(AudioSource src, float targetPitch, float duration)
    {
        if (src == null) yield break;
        float startPitch = src.pitch;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            src.pitch = Mathf.Lerp(startPitch, targetPitch, elapsed / duration);
            yield return null;
        }
    }

    private void DisableControllers()
    {
        if (moveProvider != null) moveProvider.enabled = false;
        if (turnProvider != null) turnProvider.enabled = false;
        if (snapTurnProvider != null) snapTurnProvider.enabled = false;
    }

    private IEnumerator RedFlashAndFillVision()
    {
        if (fadeImage == null) yield break;
        Color red = new Color(0.9f, 0f, 0f, 0.8f);
        Color black = new Color(0, 0, 0, 1);
        fadeImage.color = new Color(0, 0, 0, 0);

        Vector3 monsterStart = monster.transform.position;
        Vector3 cameraPos = playerCamera.position;
        cameraPos.y = monsterStart.y;

        float elapsed = 0f;
        while (elapsed < redFlashDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / redFlashDuration;
            monster.transform.position = Vector3.Lerp(monsterStart, cameraPos, t);
            FaceMonsterAtPlayer();

            if (t < 0.5f) fadeImage.color = Color.Lerp(new Color(0, 0, 0, 0), red, t * 2f);
            else fadeImage.color = Color.Lerp(red, black, (t - 0.5f) * 2f);
            yield return null;
        }
        fadeImage.color = black;
    }

    private void EnsureFadeImageExists()
    {
        if (fadeImage != null) return;
        GameObject canvasGO = new GameObject("RuntimeFadeCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32767;
        canvasGO.AddComponent<CanvasScaler>();

        GameObject imgGO = new GameObject("FadeImage");
        imgGO.transform.SetParent(canvasGO.transform, false);
        fadeImage = imgGO.AddComponent<Image>();
        fadeImage.color = new Color(0, 0, 0, 0);

        RectTransform rt = fadeImage.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private void OnFinaleComplete()
    {
        Debug.Log("[WheelchairFinale] complete");
    }
}