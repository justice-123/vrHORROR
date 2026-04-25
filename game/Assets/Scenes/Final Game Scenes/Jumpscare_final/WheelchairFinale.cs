using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;

public class WheelchairFinale : MonoBehaviour
{
    [Header("=== MONSTER ===")]
    [SerializeField] private GameObject monster;
    [SerializeField] private Animator monsterAnimator;

    [Header("=== ANIMATOR STATE NAMES ===")]
    [SerializeField] private string idleStateName = "Idle";
    [SerializeField] private string attackStateName = "Attack";

    [Header("=== CROSS-SCENE REFERENCES ===")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private string postFXGameObjectName = "HorrorPostFX";
    [SerializeField] private string redVignetteName = "RedVignette";
    [SerializeField] private string fadeImageName = "FadeImage";

    [Header("=== AUDIO ===")]
    [Tooltip("Calm outdoor ambient (birds, wind) - present from the start.")]
    [SerializeField] private AudioSource outdoorAmbient;
    [Tooltip("Tiny audio glitch during 'wrongness' phase - one wet click or distant whisper.")]
    [SerializeField] private AudioSource wrongnessGlitch;
    [Tooltip("Sub-bass that starts during wrongness, builds during void.")]
    [SerializeField] private AudioSource subBassRumble;
    [Tooltip("Breathing/wet sounds that play in 3D around player during the void.")]
    [SerializeField] private AudioSource voidBreathing;
    [Tooltip("Heartbeat that pulses during void.")]
    [SerializeField] private AudioSource heartbeatAudio;
    [Tooltip("THE BIG attack hit - low boom + impact transient.")]
    [SerializeField] private AudioSource attackBoom;
    [Tooltip("Attack screech - high frequency.")]
    [SerializeField] private AudioSource attackScreech;

    [Header("=== ACT 0: FALSE SAFETY ===")]
    [SerializeField] private float falseSafetyDuration = 4f;

    [Header("=== ACT 1: WRONGNESS ===")]
    [SerializeField] private float wrongnessDuration = 2f;
    [Tooltip("Lights to flicker during wrongness phase.")]
    [SerializeField] private Light[] flickerLights;

    [Header("=== ACT 2: SOFT TRAP ===")]
    [SerializeField] private float softTrapDuration = 1.5f;
    [SerializeField] private float fadeOutDuration = 0.6f;

    [Header("=== ACT 3: VOID ===")]
    [SerializeField] private float voidDuration = 2.5f;
    [Tooltip("How fast the breathing audio circles around the player.")]
    [SerializeField] private float breathingCircleSpeed = 1.5f;
    [Tooltip("Distance the breathing audio orbits around player.")]
    [SerializeField] private float breathingOrbitRadius = 2f;

    [Header("=== ACT 4: REVEAL + ATTACK ===")]
    [Tooltip("How CLOSE the monster spawns - keep this small (1.5-2m).")]
    [SerializeField] private float monsterRevealDistance = 1.7f;
    [SerializeField] private float fadeInDuration = 0.3f;

    [Header("=== ACT 5: MICRO-STAGGER (the magic beat) ===")]
    [Tooltip("Frozen silent moment before attack. 0.15-0.3s sweet spot.")]
    [SerializeField] private float microStaggerDuration = 0.2f;

    [Header("=== ACT 6: IMPACT ===")]
    [SerializeField] private float impactDuration = 0.3f;
    [SerializeField] private float lungeDistance = 1.4f;

    [Header("=== ACT 7: HOLD ON BLACK ===")]
    [SerializeField] private float holdOnBlackDuration = 4f;

    [Header("=== ATTACK POSITION (seated) ===")]
    [SerializeField] private bool useManualHeight = true;
    [SerializeField] private float manualHeightOverride = -0.4f;

    [Header("=== LIGHTING ===")]
    [SerializeField] private string[] scenesToDarken = { "final_jumpscare", "Second Area" };

    [Header("=== CAMERA SHAKE (short, not floaty) ===")]
    [SerializeField] private float impactShakeIntensity = 0.06f;

    [Header("=== CALIBRATION ===")]
    [SerializeField] private bool enableCalibrationMode = true;

    // Runtime
    private Transform playerCamera, playerRig;
    private ContinuousMoveProvider moveProvider;
    private ContinuousTurnProvider turnProvider;
    private SnapTurnProvider snapTurnProvider;
    private Volume horrorPostFX;
    private Image redVignetteImage;
    private Image fadeImage;

    // State
    private bool hasTriggered = false;
    private Vector3 rigShakeBasePos;

    // Cached
    private List<Light> disabledLights = new List<Light>();
    private float originalMoveSpeed;

    private void Awake()
    {
        if (monster != null) monster.SetActive(false);
    }

    private void Update()
    {
        if (!enableCalibrationMode) return;
        if (Input.GetKeyDown(KeyCode.C)) TestAttackPosition();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered) return;
        if (!other.CompareTag(playerTag)) return;
        hasTriggered = true;
        StartCoroutine(PlayCinematicScare());
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

            if (moveProvider != null) originalMoveSpeed = moveProvider.moveSpeed;
        }

        GameObject postFX = GameObject.Find(postFXGameObjectName);
        if (postFX != null) horrorPostFX = postFX.GetComponent<Volume>();

        GameObject vignette = GameObject.Find(redVignetteName);
        if (vignette != null) redVignetteImage = vignette.GetComponent<Image>();

        GameObject fade = GameObject.Find(fadeImageName);
        if (fade != null) fadeImage = fade.GetComponent<Image>();
    }

    // ============================================================
    // THE CINEMATIC SEQUENCE
    // ============================================================
    private IEnumerator PlayCinematicScare()
    {
        FindRuntimeReferences();
        EnsureFadeImageExists();

        if (redVignetteImage != null) redVignetteImage.color = new Color(0.9f, 0, 0, 0);
        if (horrorPostFX != null) horrorPostFX.weight = 0f;

        if (monster == null || monsterAnimator == null || playerCamera == null || playerRig == null)
        {
            Debug.LogError("[WheelchairFinale] missing critical refs - aborting");
            yield break;
        }

        // ============================================================
        // ACT 0: FALSE SAFETY (player thinks they escaped)
        // ============================================================
        Debug.Log("[Act 0] False safety - player feels safe");
        // Don't disable controls. Don't change anything. Let them BREATHE.
        // outdoorAmbient should already be playing (birds, wind, calm)
        if (outdoorAmbient != null && !outdoorAmbient.isPlaying) outdoorAmbient.Play();

        yield return new WaitForSeconds(falseSafetyDuration);

        // ============================================================
        // ACT 1: SUBTLE WRONGNESS (something feels off)
        // ============================================================
        Debug.Log("[Act 1] Wrongness - reality breaks slightly");

        // Audio glitch - ambient cuts out for half a second
        if (outdoorAmbient != null)
        {
            float originalVol = outdoorAmbient.volume;
            outdoorAmbient.volume = 0f;
            yield return new WaitForSeconds(0.5f);
            outdoorAmbient.volume = originalVol;
        }

        // Tiny weird sound from a random direction
        if (wrongnessGlitch != null)
        {
            PositionAudioRandomlyAroundPlayer(wrongnessGlitch, 6f);
            wrongnessGlitch.Play();
        }

        // Light flicker
        StartCoroutine(FlickerLights(wrongnessDuration));

        // Sub-bass starts very quietly
        if (subBassRumble != null)
        {
            subBassRumble.volume = 0.1f;
            subBassRumble.Play();
            StartCoroutine(RampVolume(subBassRumble, 0.4f, wrongnessDuration));
        }

        yield return new WaitForSeconds(wrongnessDuration - 0.5f);

        // ============================================================
        // ACT 2: SOFT TRAP (something takes over - not "controls disabled")
        // ============================================================
        Debug.Log("[Act 2] Soft trap - movement slows, control fades");

        // Gradually slow down movement instead of hard cut
        StartCoroutine(GraduallyDisableMovement(softTrapDuration));

        // Audio dampens - everything gets muffled
        if (outdoorAmbient != null) StartCoroutine(RampVolume(outdoorAmbient, 0.05f, softTrapDuration));

        // Sub bass swells
        if (subBassRumble != null) StartCoroutine(RampVolume(subBassRumble, 0.9f, softTrapDuration));

        yield return new WaitForSeconds(softTrapDuration);

        // Now fade to black (quickly but not instant)
        if (horrorPostFX != null) StartCoroutine(RampPostFX(1f, fadeOutDuration));
        yield return StartCoroutine(FadeColor(new Color(0, 0, 0, 0), new Color(0, 0, 0, 1), fadeOutDuration));

        // ============================================================
        // ACT 3: THE VOID (player is blind, fear builds in audio)
        // ============================================================
        Debug.Log("[Act 3] Void - audio circles, breathing, dread");

        // Stop normal world audio
        if (outdoorAmbient != null) outdoorAmbient.Stop();

        // Heartbeat starts
        if (heartbeatAudio != null)
        {
            heartbeatAudio.pitch = 1f;
            heartbeatAudio.Play();
            StartCoroutine(RampPitch(heartbeatAudio, 1.4f, voidDuration));
        }

        // Breathing audio orbits around the player - VERY important for fear
        if (voidBreathing != null)
        {
            voidBreathing.spatialBlend = 1f;
            voidBreathing.Play();
            StartCoroutine(OrbitAudioAroundPlayer(voidBreathing, voidDuration));
        }

        // Darken world while screen is black (player won't see the change)
        DarkenWorldForReveal();

        // Position monster CLOSE - 1.5-2m, not 8m away
        SpawnMonsterCloseToPlayer();
        monster.SetActive(true);
        monsterAnimator.applyRootMotion = false;
        monsterAnimator.Play(idleStateName, 0, 0f);

        yield return new WaitForSeconds(voidDuration);

        // ============================================================
        // ACT 4: REVEAL + ATTACK START (combined - no separation)
        // ============================================================
        Debug.Log("[Act 4] Reveal - monster ALREADY here");

        // Stop the void breathing
        if (voidBreathing != null) voidBreathing.Stop();
        if (heartbeatAudio != null) heartbeatAudio.Stop();

        // Position monster correctly for seated player BEFORE fade
        PositionMonsterForAttack();

        // Quick fade in - monster is right there
        yield return StartCoroutine(FadeColor(new Color(0, 0, 0, 1), new Color(0, 0, 0, 0), fadeInDuration));

        // Trigger red vignette pulse
        if (redVignetteImage != null) StartCoroutine(SingleVignettePulse(microStaggerDuration));

        // ============================================================
        // ACT 5: MICRO-STAGGER (the magic beat - dead silence)
        // ============================================================
        Debug.Log("[Act 5] Micro-stagger - frozen silence");

        // Kill ALL audio for the dead beat
        if (subBassRumble != null) subBassRumble.Stop();

        // Eye contact moment - monster is frozen, staring
        // (Animator already in Idle which is good - no movement)
        yield return new WaitForSeconds(microStaggerDuration);

        // ============================================================
        // ACT 6: VIOLENT IMPACT (everything at once)
        // ============================================================
        Debug.Log("[Act 6] IMPACT");

        // Trigger attack animation
        monsterAnimator.Play(attackStateName, 0, 0f);

        // ALL audio hits at the same frame
        if (attackBoom != null) attackBoom.Play();
        if (attackScreech != null) attackScreech.Play();

        // Short sharp camera shake
        rigShakeBasePos = playerRig.localPosition;
        StartCoroutine(ImpactShake(impactShakeIntensity, impactDuration));

        // Monster lunges into camera + red flash → black
        yield return StartCoroutine(LungeAndImpact());

        // ============================================================
        // ACT 7: HOLD ON BLACK (let it linger)
        // ============================================================
        Debug.Log("[Act 7] Hold on black");

        // No UI, no sound, just black
        yield return new WaitForSeconds(holdOnBlackDuration);

        OnFinaleComplete();
    }

    // ============================================================
    // ACT 1: FLICKER LIGHTS
    // ============================================================
    private IEnumerator FlickerLights(float duration)
    {
        if (flickerLights == null || flickerLights.Length == 0) yield break;

        bool[] originalStates = new bool[flickerLights.Length];
        for (int i = 0; i < flickerLights.Length; i++)
            if (flickerLights[i] != null) originalStates[i] = flickerLights[i].enabled;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            // Random flicker - rare but jarring
            if (Random.value < 0.08f)
            {
                foreach (Light l in flickerLights)
                    if (l != null) l.enabled = !l.enabled;

                yield return new WaitForSeconds(Random.Range(0.05f, 0.15f));

                // Restore
                for (int i = 0; i < flickerLights.Length; i++)
                    if (flickerLights[i] != null) flickerLights[i].enabled = originalStates[i];
            }

            yield return null;
        }
    }

    // ============================================================
    // ACT 2: GRADUAL MOVEMENT DISABLE (not hard cut)
    // ============================================================
    private IEnumerator GraduallyDisableMovement(float duration)
    {
        if (moveProvider == null)
        {
            DisableControllersHard();
            yield break;
        }

        float startSpeed = moveProvider.moveSpeed;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            moveProvider.moveSpeed = Mathf.Lerp(startSpeed, 0f, elapsed / duration);
            yield return null;
        }

        // Now hard disable for safety
        DisableControllersHard();
    }

    private void DisableControllersHard()
    {
        if (moveProvider != null) moveProvider.enabled = false;
        if (turnProvider != null) turnProvider.enabled = false;
        if (snapTurnProvider != null) snapTurnProvider.enabled = false;
    }

    // ============================================================
    // ACT 3: ORBIT AUDIO AROUND PLAYER (creates "something is circling me" fear)
    // ============================================================
    private IEnumerator OrbitAudioAroundPlayer(AudioSource source, float duration)
    {
        if (source == null || playerCamera == null) yield break;

        float elapsed = 0f;
        float angle = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            angle += breathingCircleSpeed * Time.deltaTime;

            Vector3 offset = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * breathingOrbitRadius;
            source.transform.position = playerCamera.position + offset;

            yield return null;
        }
    }

    // ============================================================
    // RANDOM AUDIO POSITIONING
    // ============================================================
    private void PositionAudioRandomlyAroundPlayer(AudioSource source, float distance)
    {
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * distance;
        source.transform.position = playerCamera.position + offset;
        source.spatialBlend = 1f;
    }

    // ============================================================
    // SPAWN MONSTER CLOSE (1.5-2m) IN PLAYER'S VIEW
    // ============================================================
    private void SpawnMonsterCloseToPlayer()
    {
        Vector3 lookDir = playerCamera.forward;
        lookDir.y = 0;
        lookDir.Normalize();

        Vector3 spawnPos = playerCamera.position + lookDir * monsterRevealDistance;
        spawnPos.y = playerCamera.position.y - 1.5f;

        monster.transform.position = spawnPos;
        FaceMonsterAtPlayer();
    }

    private void PositionMonsterForAttack()
    {
        Vector3 dirToPlayer = (playerCamera.position - monster.transform.position);
        dirToPlayer.y = 0;
        dirToPlayer.Normalize();

        Vector3 targetPos = playerCamera.position - dirToPlayer * monsterRevealDistance;
        monster.transform.position = targetPos;
        FaceMonsterAtPlayer();

        if (useManualHeight)
        {
            Vector3 adjusted = monster.transform.position;
            adjusted.y = playerCamera.position.y + manualHeightOverride;
            monster.transform.position = adjusted;
        }
    }

    private void FaceMonsterAtPlayer()
    {
        Vector3 lookTarget = new Vector3(playerCamera.position.x,
                                         monster.transform.position.y,
                                         playerCamera.position.z);
        monster.transform.LookAt(lookTarget);
    }

    private void TestAttackPosition()
    {
        FindRuntimeReferences();
        if (monster == null || playerCamera == null) return;

        monster.SetActive(true);
        monsterAnimator.applyRootMotion = false;
        monsterAnimator.Play(idleStateName, 0, 0f);
        SpawnMonsterCloseToPlayer();
        PositionMonsterForAttack();

        Debug.Log("[CALIBRATE] Cam Y: " + playerCamera.position.y +
                  " | Monster Y: " + monster.transform.position.y);
    }

    // ============================================================
    // ACT 6: LUNGE + IMPACT
    // ============================================================
    private IEnumerator LungeAndImpact()
    {
        Vector3 monsterStart = monster.transform.position;
        Vector3 dirToPlayer = (playerCamera.position - monsterStart);
        dirToPlayer.y = 0;
        dirToPlayer.Normalize();

        // Lunge target - very close to camera
        Vector3 lungeTarget = playerCamera.position - dirToPlayer * 0.3f;
        if (useManualHeight) lungeTarget.y = playerCamera.position.y + manualHeightOverride;

        Color red = new Color(0.9f, 0f, 0f, 0.7f);
        Color black = new Color(0, 0, 0, 1);

        if (fadeImage != null) fadeImage.color = new Color(0, 0, 0, 0);

        float elapsed = 0f;
        while (elapsed < impactDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / impactDuration;

            monster.transform.position = Vector3.Lerp(monsterStart, lungeTarget, t);
            FaceMonsterAtPlayer();

            // Red flash → black
            if (fadeImage != null)
            {
                if (t < 0.5f) fadeImage.color = Color.Lerp(new Color(0, 0, 0, 0), red, t * 2f);
                else fadeImage.color = Color.Lerp(red, black, (t - 0.5f) * 2f);
            }

            yield return null;
        }

        if (fadeImage != null) fadeImage.color = black;
    }

    // ============================================================
    // CAMERA SHAKE (short, sharp, not floaty)
    // ============================================================
    private IEnumerator ImpactShake(float intensity, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float remaining = 1f - (elapsed / duration); // shake fades out
            Vector3 shake = Random.insideUnitSphere * intensity * remaining;
            shake.y *= 0.3f; // less vertical (nausea)
            playerRig.localPosition = rigShakeBasePos + shake;
            yield return null;
        }
        playerRig.localPosition = rigShakeBasePos;
    }

    // ============================================================
    // VIGNETTE
    // ============================================================
    private IEnumerator SingleVignettePulse(float duration)
    {
        if (redVignetteImage == null) yield break;

        Color off = new Color(0.9f, 0f, 0f, 0f);
        Color on = new Color(0.9f, 0f, 0f, 0.6f);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float pulse = Mathf.Sin(t * Mathf.PI); // single sine pulse
            redVignetteImage.color = Color.Lerp(off, on, pulse);
            yield return null;
        }
        redVignetteImage.color = off;
    }

    // ============================================================
    // LIGHTING (just darken - no red ramp during void)
    // ============================================================
    private void DarkenWorldForReveal()
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
        RenderSettings.ambientLight = new Color(0.1f, 0.02f, 0.02f);
        RenderSettings.ambientIntensity = 0.15f;
    }

    // ============================================================
    // POST PROCESSING + AUDIO HELPERS
    // ============================================================
    private IEnumerator RampPostFX(float targetWeight, float duration)
    {
        if (horrorPostFX == null) yield break;
        float start = horrorPostFX.weight;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            horrorPostFX.weight = Mathf.Lerp(start, targetWeight, elapsed / duration);
            yield return null;
        }
        horrorPostFX.weight = targetWeight;
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

    private IEnumerator FadeColor(Color startColor, Color endColor, float duration)
    {
        if (fadeImage == null) yield break;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            fadeImage.color = Color.Lerp(startColor, endColor, elapsed / duration);
            yield return null;
        }
        fadeImage.color = endColor;
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
        Debug.Log("[WheelchairFinale] cinematic complete");
    }
}