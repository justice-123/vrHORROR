using System.Collections;
using UnityEngine;
using TMPro;

public class TutorialUI : MonoBehaviour
{
    public TMP_Text mainText;
    public CanvasGroup canvasGroup;

    public TMP_Text pressAText;

    Coroutine blinkCoroutine;

    public void Show(string main)
    {
        StopAllCoroutines();
        StopBlink();
        mainText.text = main;
        StartCoroutine(Fade(0f, 1f, 0.3f));
    }

    // Added: show text and start blinking the Press A hint
    public void ShowWithPrompt(string main)
    {
        StopAllCoroutines();
        StopBlink();
        mainText.text = main;
        StartCoroutine(Fade(0f, 1f, 0.3f));
        StartBlink();
    }

    public void ShowSuccess()
    {
        StopAllCoroutines();
        StopBlink();
        mainText.text = "Try to escape here!";
        StartCoroutine(FadeOutAfter(2.5f));
    }

    public void Hide()
    {
        StopBlink();
        StartCoroutine(Fade(canvasGroup.alpha, 0f, 0.3f));
    }

    // Added: start the blink loop
    void StartBlink()
    {
        if (pressAText == null) return;
        pressAText.gameObject.SetActive(true);
        pressAText.text = "Press A to continue";
        pressAText.alpha = 1f;
        blinkCoroutine = StartCoroutine(BlinkPressA());
    }

    // Added: stop the blink loop and hide the hint
    void StopBlink()
    {
        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
            blinkCoroutine = null;
        }
        if (pressAText != null)
            pressAText.gameObject.SetActive(false);
    }

    // Added: blink the Press A text on and off
    IEnumerator BlinkPressA()
    {
        float blinkInterval = 0.5f;
        while (true)
        {
            pressAText.alpha = 1f;
            yield return new WaitForSeconds(blinkInterval);
            pressAText.alpha = 0f;
            yield return new WaitForSeconds(blinkInterval);
        }
    }

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