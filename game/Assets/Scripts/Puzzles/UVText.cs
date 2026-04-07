using System.Collections;
using Oculus.Platform.Models;
using TMPro;
using UnityEngine;

public class UVText : MonoBehaviour
{

    public TextMeshPro tmp;
    public Color offColour = new Color(0.517f, 0.113f, 0.749f, 0f); 
    public Color uvColour = new Color(0.517f, 0.113f, 0.749f, 1f);

    private Coroutine lerp;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        tmp.color = offColour;
    }

    public void turnTextOn()
    {
        
        lerp = StartCoroutine(LerpToColour(uvColour));
    }

    public void turnTextOff()
    {
        lerp = StartCoroutine(LerpToColour(offColour));
    }

    private void StopLerp()
    {
        if (lerp != null) StopCoroutine(lerp);
    }

    IEnumerator LerpToColour(Color target)
    {
        float duration = 5.0f;
        float elapsed = 0f;
        Color startColour = tmp.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            tmp.color = Color.Lerp(startColour, target, t);

            yield return null;
        }

        tmp.color = target;
    }



    
}
