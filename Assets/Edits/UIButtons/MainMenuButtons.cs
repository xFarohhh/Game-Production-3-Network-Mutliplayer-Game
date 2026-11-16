// EDITED SCRIPT - moved from Assets/KeaganWork/UI/MainMenuButtons.cs (original: Edits/Originals_Backup/MainMenuButtons.cs.bak).
// changed: null-safe (the MainMenu scene never assigns CreateLobbyScreen / chooseUserNameScreen, Awake used to throw)
// changed: Host Game / Join Lobby open the lobby screens built by LobbyMenuUI; every old public method is kept
using UnityEngine;
using UnityEngine.UI;

public class MainMenuButtons : MonoBehaviour
{
    [Header("Screens")]
    [SerializeField] GameObject JoinLobbyScreen;
    [SerializeField] GameObject CreateLobbyScreen;
    [SerializeField] GameObject chooseUserNameScreen;

    [Header("Buttons")]
    [SerializeField] Button[] menuButtons; //  setting buttons as an array just to it is easier to set them all active and inactive when specific screens are open
                                           //  to prevent the player from pressing the buttons when other screens are open

    private void Awake()
    {
        // changed: null checks
        if (JoinLobbyScreen != null) JoinLobbyScreen.SetActive(false);
        if (CreateLobbyScreen != null) CreateLobbyScreen.SetActive(false);
        if (chooseUserNameScreen != null) chooseUserNameScreen.SetActive(false);
    }

    // changed: "Host Game" -> username + role + create lobby screen (then the waiting room)
    public void hostLobbyButton()
    {
        SetMenuButtons(false);
        if (LobbyMenuUI.Instance != null) LobbyMenuUI.Instance.ShowHostSetup();
        else Debug.LogWarning("MainMenuButtons: LobbyMenuUI is not in the scene.");
    }

    // changed: "Join Lobby" -> username + lobby code screen (then the waiting room)
    public void joinLobbyButton()
    {
        SetMenuButtons(false);
        if (LobbyMenuUI.Instance != null) LobbyMenuUI.Instance.ShowJoin();
        else Debug.LogWarning("MainMenuButtons: LobbyMenuUI is not in the scene.");
    }

    public void joinLobbybackButton()
    {
        if (JoinLobbyScreen != null) JoinLobbyScreen.SetActive(false);   // changed: null-safe
        SetMenuButtons(true);
    }
    public void joinLobbyCreateLobbyBackButton()
    {
        if (CreateLobbyScreen != null) CreateLobbyScreen.SetActive(false);   // changed: null-safe
        SetMenuButtons(true);
    }
    public void createUserNameContinueButton()
    {
        if (chooseUserNameScreen != null) chooseUserNameScreen.SetActive(false);   // changed: null-safe
        if (CreateLobbyScreen != null) CreateLobbyScreen.SetActive(true);
    }

    // added: the old "Input Lobby Code" button -> the join screen where the code is typed
    public void inputLobbyCodeButton()
    {
        joinLobbyButton();
    }

    // added: the old "Start game" button - only the host can start, and only with 2+ players
    public void startGameButton()
    {
        if (LobbyManager.Instance != null) LobbyManager.Instance.StartGame();
    }

    // added: back to the main menu from any lobby screen (called by LobbyMenuUI)
    public void backToMainMenuButton()
    {
        if (JoinLobbyScreen != null) JoinLobbyScreen.SetActive(false);
        if (CreateLobbyScreen != null) CreateLobbyScreen.SetActive(false);
        if (chooseUserNameScreen != null) chooseUserNameScreen.SetActive(false);
        SetMenuButtons(true);
    }

    // added: helper replacing the repeated foreach loops (null-safe)
    private void SetMenuButtons(bool active)
    {
        if (menuButtons == null) return;
        foreach (var button in menuButtons)
        {
            if (button != null) button.gameObject.SetActive(active);
        }
    }

    // changed: Application.Quit does nothing in the editor, so stop play mode there
    public void OnApplicationQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
