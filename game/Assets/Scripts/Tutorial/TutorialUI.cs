using System.Collections;
using UnityEngine;
using TMPro;

public class TutorialUI : MonoBehaviour
{
    public TMP_Text mainText;
    public CanvasGroup canvasGroup;

    public void Show(string main)
    {
        StopAllCoroutines();
        mainText.text = main;
        StartCoroutine(Fade(0f, 1f, 0.3f));
    }

    public void ShowSuccess()
    {
        StopAllCoroutines();
        mainText.text = "Try to escape here";
        StartCoroutine(FadeOutAfter(2.5f));
    }

    public void Hide() => StartCoroutine(Fade(canvasGroup.alpha, 0f, 0.3f));

    IEnumerator Fade(float from, float to, float dur)
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / dur;
            canvasGroup.alpha = Mathf.Lerp(from, to, t);
            yield return null;
        }
        canvasGroup.alpha = to;
    }

    IEnumerator FadeOutAfter(float delay)
    {
        yield return new WaitForSeconds(delay);
        yield return StartCoroutine(Fade(1f, 0f, 0.4f));
    }
}