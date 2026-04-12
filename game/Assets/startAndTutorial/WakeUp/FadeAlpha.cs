using UnityEngine;

public class FadeAlpha : MonoBehaviour
{
    [Tooltip("Fade duration in seconds")]
    public float fadeTime = 2f;

    private Renderer _renderer;
    private float _currentTime; 

    void Start()
    {
        _renderer = GetComponent<Renderer>();
        if (_renderer == null)
        {
            Debug.LogError("NO Renderer！");
            enabled = false; 
            return;
        }

        Color initColor = _renderer.material.color;
        initColor.a = 1f;
        _renderer.material.color = initColor;
    }

    void Update()
    {
        if (_currentTime < fadeTime && _renderer != null)
        {
            _currentTime += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, _currentTime / fadeTime);

            Color newColor = _renderer.material.color;
            newColor.a = alpha;
            _renderer.material.color = newColor;
        }
    }

    public void TriggerFade()
    {
        _currentTime = 0f; 
    }
}