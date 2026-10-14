using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerPauseHandler : MonoBehaviour
{
    [Header("Player Input Action Asset")]
    private PlayerInput playerInput;

    [Header("Ui Screens")]
    [SerializeField] GameObject pauseScreen;
    [SerializeField] GameObject SettingsScreen;

    [Header("Input Variables")]
    private bool isPaused;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();  
    }

    private void OnEnable()
    {
        pauseScreen = GameObject.FindGameObjectWithTag("PauseScreen");
        SettingsScreen = GameObject.FindGameObjectWithTag("settingsScreen");
    }

    public void OnPause(InputAction.CallbackContext context)
    {
        isPaused = context.performed;
        pauseScreen.SetActive(context.action.WasPerformedThisFrame());
    }

    public void openSettingsScreen()
    {
        SettingsScreen.SetActive(true);
        pauseScreen.SetActive(false);
    }

    public void closeSettingsScreen()
    {
        SettingsScreen.SetActive(false);
        pauseScreen.SetActive(true);
    }
}

