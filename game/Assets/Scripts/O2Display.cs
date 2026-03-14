using UnityEngine;
using TMPro;

public class O2Display : MonoBehaviour
{
    public OxygenTank tank;
    public TextMeshProUGUI oxygenText;

    void Update()
    {
        if (tank == null || oxygenText == null) return;

        tank.UseOxygen(10f * Time.deltaTime);   // temporary test

        oxygenText.text = Mathf.RoundToInt(tank.oxygenLevel) + "%";
    }
}