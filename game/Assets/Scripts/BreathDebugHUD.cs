using UnityEngine;

/// <summary>
/// Debug overlay for BreathInput values.
/// Uses OnGUI for maximum compatibility - no Canvas, no TextMeshPro needed.
/// Shows as a 2D screen overlay that works in VR as a head-locked display.
///
/// Drop on any GameObject and assign your BreathInput reference.
/// Remove this script before shipping - it's just for tuning.
/// </summary>
public class BreathDebugHUD : MonoBehaviour
{
    [Header("References")]
    public BreathInput breath;

    [Header("Display")]
    [Tooltip("Size multiplier for the text")]
    [Range(1f, 5f)]
    public float scale = 2f;

    private GUIStyle boxStyle;
    private GUIStyle textStyle;
    private bool stylesReady;

    void SetupStyles()
    {
        boxStyle = new GUIStyle(GUI.skin.box);
        boxStyle.normal.background = MakeTex(2, 2, new Color(0f, 0f, 0f, 0.85f));

        textStyle = new GUIStyle(GUI.skin.label);
        textStyle.normal.textColor = Color.green;
        textStyle.richText = true;

        stylesReady = true;
    }

    void OnGUI()
    {
        if (breath == null) return;
        if (!stylesReady) SetupStyles();

        int baseFontSize = (int)(14 * scale);
        textStyle.fontSize = baseFontSize;

        float w = 320 * scale;
        float h = 300 * scale;
        float x = 10;
        float y = 10;

        // Figure out current state
        string state;
        Color stateColor;
        if (breath.debugRms < breath.silenceRms)
        {
            state = "SILENCE";
            stateColor = Color.gray;
        }
        else if (breath.isBreathing)
        {
            state = "BREATH";
            stateColor = Color.cyan;
        }
        else
        {
            state = "SPEECH";
            stateColor = Color.yellow;
        }

        string hex = ColorUtility.ToHtmlStringRGB(stateColor);

        string text =
            $"<b>=== Breath Debug ===</b>\n" +
            $"State: <color=#{hex}><b>{state}</b></color>\n" +
            $"\n" +
            $"RMS:         {breath.debugRms:F6}\n" +
            $"Pitch Conf:  {breath.debugPitchConfidence:F3}\n" +
            $"Pitch Hz:    {breath.debugPitchHz:F1}\n" +
            $"\n" +
            $"Breathing:   {breath.isBreathing}\n" +
            $"Intensity:   {breath.breathIntensity01:F2}\n" +
            $"BPM:         {breath.breathsPerMinute:F1}\n" +
            $"\n" +
            $"<b>--- Thresholds ---</b>\n" +
            $"Silence RMS: {breath.silenceRms:F4}\n" +
            $"Pitch Thr:   {breath.pitchConfidenceThreshold:F2}\n" +
            $"Heavy RMS:   {breath.heavyBreathRms:F4}";

        GUI.Box(new Rect(x, y, w, h), "", boxStyle);
        GUI.Label(new Rect(x + 10, y + 5, w - 20, h - 10), text, textStyle);
    }

    // Helper: create a solid color texture for the background
    Texture2D MakeTex(int width, int height, Color color)
    {
        Color[] pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = color;
        Texture2D tex = new Texture2D(width, height);
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }
}