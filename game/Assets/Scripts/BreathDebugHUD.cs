using UnityEngine;
using TMPro;

public class BreathDebugHUD : MonoBehaviour
{
    public BreathInput breath;
    public TextMeshProUGUI debugText;

    void Update()
    {
        if (breath == null || debugText == null) return;

        debugText.text =
            "Zone: " + breath.debugZone + "\n" +
            "\n" +
            "RMS:        " + breath.debugRms.ToString("F6") + "\n" +
            "Pitch Conf: " + breath.debugPitchConfidence.ToString("F3") + "\n" +
            "Pitch Hz:   " + breath.debugPitchHz.ToString("F1") + "\n" +
            "\n" +
            "Breathing:  " + breath.isBreathing + "\n" +
            "Intensity:  " + breath.breathIntensity01.ToString("F2") + "\n" +
            "BPM:        " + breath.breathsPerMinute.ToString("F1");
    }
}