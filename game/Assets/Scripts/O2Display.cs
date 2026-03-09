using UnityEngine;
using TMPro;

public class O2Display : MonoBehaviour
{
    public OxygenTank tank;
    public TextMeshProUGUI oxygenText;

    // Update is called once per frame
    void Update()
    {
        if (tank == null || oxygenText == null) return;

        oxygenText.text = Mathf.RoundToInt(tank.oxygenLevel) + "%";
    }
}
