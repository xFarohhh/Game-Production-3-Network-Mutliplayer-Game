using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingsUIhandler : MonoBehaviour
{
    [SerializeField] PlayerLocomotion playerLocomotion;

    [Header("Settings screen Gameobject")]
    [SerializeField] GameObject settingsScreen;

    [Header("Gamepad sensitivity")]
    [SerializeField] Slider gamepadSlider;
    [SerializeField] TextMeshProUGUI gamepadText;

    [Header("Mouse sensitivity")]
    [SerializeField] Slider mouseSlider;
    [SerializeField] TextMeshProUGUI mouseSensitivityText;


    public void handleSensitivitySliders()
    {
        #region Gamepad
        gamepadSlider.value = playerLocomotion.getGamepadSens();
        gamepadText.text = gamepadSlider.value.ToString();
        #endregion Gamepad

        #region Mouse
        mouseSlider.value = playerLocomotion.getMouseSens();
        mouseSensitivityText.text = mouseSlider.value.ToString();
        #endregion Mouse
    }
}
