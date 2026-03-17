using UnityEngine;
using TMPro;

public class BreathDebugHUD : MonoBehaviour
{
    public BreathInputML breath;
    public TextMeshProUGUI debugText;

    void Update()
    {
        if (breath == null || debugText == null) return;

        string color;
        if (breath.detectedClass == "inhale")
            color = "#00FFFF";
        else if (breath.detectedClass == "exhale")
            color = "#FF8800";
        else if (breath.detectedClass == "silence")
            color = "#888888";
        else
            color = "#FFFF00";

        debugText.text =
            "<b>ML Breath Debug</b>\n" +
            "Class: <color=" + color + ">" + breath.detectedClass + "</color>\n" +
            "\n" +
            "Inhale:   " + (breath.inhaleConfidence * 100f).ToString("F1") + "%\n" +
            "Exhale:   " + (breath.exhaleConfidence * 100f).ToString("F1") + "%\n" +
            "Silence:  " + (breath.silenceConfidence * 100f).ToString("F1") + "%\n" +
            "\n" +
            "Breathing: " + breath.isBreathing + "\n" +
            "Intensity: " + breath.breathIntensity01.ToString("F2") + "\n" +
            "RMS:       " + breath.debugRms.ToString("F6");
    }
}