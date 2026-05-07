using System.Collections;
using UnityEngine;
using UnityEngine.UI;

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