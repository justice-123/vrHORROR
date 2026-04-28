using UnityEngine;

public class UVSwitch : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public UVLamp uvLamp;
    public KeypadLight keypadLight;

    private bool canToggle = true;
    public float cooldown = 1f;

    public bool hintTriggered = false;

    public void pressSwitch()
    {

        if (hintTriggered == false)
        {
            hintTriggered = true;
            HintButton.Instance.changeHintState(HintButton.HintState.ObtainLiftKey);
        }

        keypadLight.enableKeyPadLight();

        if (canToggle)
        {//flips the switch
            Vector3 scale = transform.localScale;
            scale.y *= -1f;
            transform.localScale = scale;

            //turn uv light on
            uvLamp.ToggleLight();

            canToggle = false;

            Invoke(nameof(ResetToggle), cooldown);
        }
    }

    private void ResetToggle() => canToggle = true;
}
