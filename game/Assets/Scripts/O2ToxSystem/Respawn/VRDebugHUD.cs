using UnityEngine;
using TMPro;

public class VRDebugHUD : MonoBehaviour
{
    public static VRDebugHUD Instance { get; private set; }

    [SerializeField] private TMP_Text debugText;

    private string _status = "";

    void Awake() { Instance = this; }

    void Update()
    {
        if (debugText == null) return;

        float o2 = OxygenTank.Instance != null ? OxygenTank.Instance.oxygenLevel : -1f;

        debugText.text =
            $"O2: {o2:F1}\n" +
            $"OxygenTank: {(OxygenTank.Instance != null ? "OK" : "NULL")}\n" +
            $"OxygenMgr: {(OxygenManager.Instance != null ? "OK" : "NULL")}\n" +
            $"RespawnMgr: {(RespawnManager.Instance != null ? "OK" : "NULL")}\n" +
            $"Spawn1: {(RespawnManager.Instance?.FirstAreaSpawn != null ? "OK" : "NULL")}\n" +
            $"Spawn2: {(RespawnManager.Instance?.SecondAreaSpawn != null ? "OK" : "NULL")}\n" +
            $"Status: {_status}";
    }

    public void SetStatus(string msg) { _status = msg; }
}