using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attach to the same GameObject as a full-screen UI Image on your XR rig.
/// Fades the screen to black and back again.
///
/// Setup:
///   1. In your XR Rig hierarchy, create: XR Rig > ScreenFader (empty GO)
///   2. Add a Canvas (Render Mode: World Space) to ScreenFader
///   3. Set Canvas width/height to something like 2x2, move it ~0.3 units
///      in front of the camera (z = 0.3), scale it down to fill FOV
///   4. Add a child Image that fills the Canvas, set color to black
///   5. Add THIS script to ScreenFader and drag the Image into the slot below
/// </summary>
public class ScreenFader : MonoBehaviour
{
    public static ScreenFader Instance { get; private set; }

    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeDuration = 0.5f;

    void Awake()
    {
        Instance = this;
        // Start fully transparent
        SetAlpha(0f);
    }

    public void FadeToBlack(System.Action onComplete = null)
    {
        StartCoroutine(Fade(0f, 1f, onComplete));
    }

    public void FadeToClear(System.Action onComplete = null)
    {
        StartCoroutine(Fade(1f, 0f, onComplete));
    }

    private IEnumerator Fade(float from, float to, System.Action onComplete)
    {
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            SetAlpha(Mathf.Lerp(from, to, t / fadeDuration));
            yield return null;
        }
        SetAlpha(to);
        onComplete?.Invoke();
    }

    private void SetAlpha(float a)
    {
        if (fadeImage == null) return;
        Color c = fadeImage.color;
        c.a = a;
        fadeImage.color = c;
    }
}