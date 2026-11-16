// ADDED SCRIPT - new file (Assets/Edits/Lobby/Additions/LobbyMenuUI.cs). Adapted to this project's LobbyManager / LobbySession / MapList names.
// Drives the three lobby screens that now live in the MainMenu scene (Canvas > LobbyMenuUI):
//   1. Host setup  - host picks a username and a role (Human / Monster), then creates the lobby
//   2. Join        - player enters a username and the lobby code
//   3. Waiting room- everybody sees the players, their roles, the chosen map and game mode; the host can change
//                    the map / game mode, and once there is more than one person the host gets Start Game.
// changed: the screens used to be built at runtime. They are now ordinary scene objects that are assigned in
// the Inspector and kept inactive until needed - restyle / move them freely in the editor.
using System;
using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class LobbyMenuUI : MonoBehaviour   // added
{
    public static LobbyMenuUI Instance { get; private set; }   // added

    private static readonly Color ButtonColor = new Color(0.20f, 0.22f, 0.28f, 1f);
    private static readonly Color ButtonSelected = new Color(0.20f, 0.55f, 0.30f, 1f);

    // ---------- screens (inactive until needed) ----------
    [Header("Screens")]
    [SerializeField] private GameObject hostSetupScreen;       // changed: assigned in the Inspector
    [SerializeField] private GameObject joinScreen;
    [SerializeField] private GameObject waitingScreen;

    [Header("Host setup screen")]
    [SerializeField] private TMP_InputField hostNameInput;
    [SerializeField] private Button humanRoleButton;
    [SerializeField] private Button monsterRoleButton;
    [SerializeField] private Button createButton;
    [SerializeField] private Button hostBackButton;
    [SerializeField] private TMP_Text hostStatusText;

    [Header("Join screen")]
    [SerializeField] private TMP_InputField joinNameInput;
    [SerializeField] private TMP_InputField joinCodeInput;
    [SerializeField] private Button joinButton;
    [SerializeField] private Button joinBackButton;
    [SerializeField] private TMP_Text joinStatusText;

    [Header("Waiting room screen")]
    [SerializeField] private TMP_Text codeText;
    [SerializeField] private TMP_Text playerCountText;
    [SerializeField] private TMP_Text[] playerRows;            // one text per slot (5 = LobbyManager.MAX_PLAYERS)
    [SerializeField] private TMP_Text mapText;
    [SerializeField] private TMP_Text modeText;
    [SerializeField] private TMP_Text myRoleText;
    [SerializeField] private TMP_Text waitStatusText;
    [SerializeField] private Button changeMapButton;
    [SerializeField] private Button changeModeButton;
    [SerializeField] private Button startButton;
    [SerializeField] private Button leaveButton;

    [Header("Message at the top of the screen")]
    [SerializeField] private TMP_Text toastText;

    private MainMenuButtons menuButtons;
    private LobbyManager.PlayerRole? chosenRole;
    private float toastTimer;
    private bool wired;

    private LobbyManager Manager => LobbyManager.Instance;   // named Manager so it does not clash with the Lobby model type

    // =====================================================================================
    private void Awake()
    {
        Instance = this;

        // coming back from a game: make sure the menu is usable again
        LobbySession.ClearLobby();
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // changed: button clicks are hooked up in code (the screens themselves can be edited freely)
        Wire(humanRoleButton, () => ChooseRole(LobbyManager.PlayerRole.Human));
        Wire(monsterRoleButton, () => ChooseRole(LobbyManager.PlayerRole.Monster));
        Wire(createButton, OnCreateClicked);
        Wire(hostBackButton, BackToMainMenu);
        Wire(joinButton, OnJoinClicked);
        Wire(joinBackButton, BackToMainMenu);
        Wire(changeMapButton, OnChangeMapClicked);
        Wire(changeModeButton, OnChangeModeClicked);
        Wire(startButton, OnStartClicked);
        Wire(leaveButton, OnLeaveClicked);
        wired = true;
    }

    private static void Wire(Button b, UnityEngine.Events.UnityAction action)
    {
        if (b != null) b.onClick.AddListener(action);
        else Debug.LogWarning("LobbyMenuUI: a button reference is not assigned in the Inspector.");
    }

    private void Start()
    {
        menuButtons = FindFirstObjectByType<MainMenuButtons>(FindObjectsInactive.Include);

        // everything starts hidden
        SetActiveSafe(hostSetupScreen, false);
        SetActiveSafe(joinScreen, false);
        SetActiveSafe(waitingScreen, false);
        SetActiveSafe(toastText != null ? toastText.gameObject : null, false);

        if (Manager != null)
        {
            Manager.OnJoinedLobby += OnJoinedLobby;
            Manager.OnJoinedLobbyUpdate += OnLobbyUpdated;
            Manager.OnLobbyGameModeChanged += OnLobbyUpdated;   // refresh right after the host changes the mode
            Manager.OnLeftLobby += OnLeftLobby;
            Manager.OnKickedFromLobby += OnKicked;
            Manager.OnLobbyError += OnLobbyError;
            Manager.OnGameStarting += OnGameStarting;

            // returning from an old lobby - make sure the right screen is up
            if (Manager.GetJoinedLobby() != null) ShowWaitingRoom(Manager.GetJoinedLobby());
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (Manager != null)
        {
            Manager.OnJoinedLobby -= OnJoinedLobby;
            Manager.OnJoinedLobbyUpdate -= OnLobbyUpdated;
            Manager.OnLobbyGameModeChanged -= OnLobbyUpdated;
            Manager.OnLeftLobby -= OnLeftLobby;
            Manager.OnKickedFromLobby -= OnKicked;
            Manager.OnLobbyError -= OnLobbyError;
            Manager.OnGameStarting -= OnGameStarting;
        }
    }

    private void Update()
    {
        if (toastText != null && toastText.gameObject.activeSelf)
        {
            toastTimer -= Time.unscaledDeltaTime;
            if (toastTimer <= 0f) toastText.gameObject.SetActive(false);
        }
    }

    private static void SetActiveSafe(GameObject go, bool active)
    {
        if (go != null) go.SetActive(active);
    }

    private static void SetText(TMP_Text t, string value)
    {
        if (t != null) t.text = value;
    }

    // =====================================================================================
    // public entry points used by MainMenuButtons
    // =====================================================================================
    public void ShowHostSetup()
    {
        SetText(hostStatusText, "");
        SetActiveSafe(hostSetupScreen, true);
        SetActiveSafe(joinScreen, false);
        SetActiveSafe(waitingScreen, false);
    }

    public void ShowJoin()
    {
        SetText(joinStatusText, "");
        SetActiveSafe(joinScreen, true);
        SetActiveSafe(hostSetupScreen, false);
        SetActiveSafe(waitingScreen, false);
    }

    private void ShowWaitingRoom(Lobby lobby)
    {
        SetActiveSafe(hostSetupScreen, false);
        SetActiveSafe(joinScreen, false);
        SetActiveSafe(waitingScreen, true);
        RefreshWaitingRoom(lobby);
    }

    private void BackToMainMenu()
    {
        SetActiveSafe(hostSetupScreen, false);
        SetActiveSafe(joinScreen, false);
        SetActiveSafe(waitingScreen, false);
        if (menuButtons != null) menuButtons.backToMainMenuButton();
    }

    // =====================================================================================
    // lobby events
    // =====================================================================================
    private void OnJoinedLobby(object sender, LobbyManager.LobbyEventArgs e)
    {
        ShowWaitingRoom(e.lobby);
    }

    private void OnLobbyUpdated(object sender, LobbyManager.LobbyEventArgs e)
    {
        if (waitingScreen != null && waitingScreen.activeSelf && e.lobby != null) RefreshWaitingRoom(e.lobby);
    }

    private void OnLeftLobby(object sender, EventArgs e)
    {
        BackToMainMenu();
    }

    private void OnKicked(object sender, LobbyManager.LobbyEventArgs e)
    {
        BackToMainMenu();
        Toast("The lobby was closed or you were removed from it.");
    }

    private void OnLobbyError(string message)
    {
        if (hostSetupScreen != null && hostSetupScreen.activeSelf) SetText(hostStatusText, message);
        else if (joinScreen != null && joinScreen.activeSelf) SetText(joinStatusText, message);
        else Toast(message);
    }

    private void OnGameStarting(object sender, EventArgs e)
    {
        Toast("Starting game...");
    }

    // =====================================================================================
    // button handlers
    // =====================================================================================
    private void ChooseRole(LobbyManager.PlayerRole role)
    {
        chosenRole = role;
        if (humanRoleButton != null) humanRoleButton.GetComponent<Image>().color = role == LobbyManager.PlayerRole.Human ? ButtonSelected : ButtonColor;
        if (monsterRoleButton != null) monsterRoleButton.GetComponent<Image>().color = role == LobbyManager.PlayerRole.Monster ? ButtonSelected : ButtonColor;
        SetText(hostStatusText, "");
    }

    private async void OnCreateClicked()
    {
        string userName = CleanName(hostNameInput != null ? hostNameInput.text : "");
        if (userName.Length == 0) { SetText(hostStatusText, "Please enter a username."); return; }
        if (chosenRole == null) { SetText(hostStatusText, "Please choose a role: Human or Monster."); return; }
        if (Manager == null) { SetText(hostStatusText, "LobbyManager is missing from the scene."); return; }

        createButton.interactable = false;
        SetText(hostStatusText, "Creating lobby...");
        bool ok = await Manager.CreateLobbyAsync(userName, chosenRole.Value, MapList.defaultMap.mapName);
        if (this == null) return;
        createButton.interactable = true;
        if (ok) SetText(hostStatusText, "");   // OnJoinedLobby switches to the waiting room
    }

    private async void OnJoinClicked()
    {
        string userName = CleanName(joinNameInput != null ? joinNameInput.text : "");
        string code = (joinCodeInput != null ? joinCodeInput.text : "").Trim();
        if (userName.Length == 0) { SetText(joinStatusText, "Please enter a username."); return; }
        if (code.Length == 0) { SetText(joinStatusText, "Please enter the lobby code."); return; }
        if (Manager == null) { SetText(joinStatusText, "LobbyManager is missing from the scene."); return; }

        joinButton.interactable = false;
        SetText(joinStatusText, "Joining lobby...");
        bool ok = await Manager.JoinLobbyByCodeAsync(userName, code);
        if (this == null) return;
        joinButton.interactable = true;
        if (ok) SetText(joinStatusText, "");
    }

    private void OnChangeMapClicked()
    {
        if (Manager == null || !Manager.IsLobbyHost()) return;
        Manager.changeMap(MapList.nextMap(LobbyManager.getMap(Manager.GetJoinedLobby())));
    }

    // host cycles Last Man Standing <-> Escape
    private void OnChangeModeClicked()
    {
        if (Manager != null && Manager.IsLobbyHost()) Manager.ChangeGamemode();
    }

    private void OnStartClicked()
    {
        if (Manager != null && Manager.canHostStartGame())
        {
            startButton.interactable = false;
            SetText(waitStatusText, "Starting game...");
            Manager.StartGame();
        }
    }

    private void OnLeaveClicked()
    {
        if (Manager != null) Manager.LeaveLobby();
        else BackToMainMenu();
    }

    private static string CleanName(string raw)
    {
        string n = (raw ?? "").Trim();
        return n.Length > 20 ? n.Substring(0, 20) : n;
    }

    // =====================================================================================
    // waiting room refresh
    // =====================================================================================
    private void RefreshWaitingRoom(Lobby lobby)
    {
        bool isHost = Manager != null && Manager.IsLobbyHost();

        SetText(codeText, "Lobby Code:  " + lobby.LobbyCode);
        SetText(playerCountText, "Players  " + lobby.Players.Count + " / " + LobbyManager.MAX_PLAYERS);
        SetText(mapText, MapList.displayMapName(LobbyManager.getMap(lobby)));
        SetText(modeText, LobbyManager.setGameModeDisplay(LobbyManager.GetGameMode(lobby)));

        // host first, then by join order
        System.Collections.Generic.List<Player> ordered = new System.Collections.Generic.List<Player>(lobby.Players);
        ordered.Sort((a, b) =>
        {
            if (a.Id == lobby.HostId) return -1;
            if (b.Id == lobby.HostId) return 1;
            return a.Joined.CompareTo(b.Joined);
        });

        string myId = Unity.Services.Authentication.AuthenticationService.Instance.PlayerId;
        if (playerRows != null)
        {
            for (int i = 0; i < playerRows.Length; i++)
            {
                TMP_Text row = playerRows[i];
                if (row == null) continue;
                if (i < ordered.Count)
                {
                    Player p = ordered[i];
                    string pName = "Player";
                    if (p.Data != null && p.Data.TryGetValue(LobbyManager.KEY_PLAYER_NAME, out PlayerDataObject nameData)) pName = nameData.Value;
                    LobbyManager.PlayerRole role = LobbyManager.getPlayerRole(lobby, p.Id);
                    string tags = (p.Id == lobby.HostId ? "  (Host)" : "") + (p.Id == myId ? "  (You)" : "");
                    string roleColor = role == LobbyManager.PlayerRole.Monster ? "#F25A5A" : "#8CD9FF";
                    row.text = pName + tags + "    <color=" + roleColor + ">" + role + "</color>";
                }
                else
                {
                    row.text = "<color=#666666>Empty slot</color>";
                }
            }
        }

        LobbyManager.PlayerRole mine = LobbyManager.getPlayerRole(lobby, myId);
        SetText(myRoleText, "You are: <color=" + (mine == LobbyManager.PlayerRole.Monster ? "#F25A5A" : "#8CD9FF") + ">" + mine + "</color>");

        SetActiveSafe(changeMapButton != null ? changeMapButton.gameObject : null, isHost);
        SetActiveSafe(changeModeButton != null ? changeModeButton.gameObject : null, isHost);   // only the host can change the game mode
        bool canStart = isHost && lobby.Players.Count > 1;
        if (startButton != null)
        {
            startButton.gameObject.SetActive(canStart);          // appears once more than one person is in the lobby
            startButton.interactable = true;
        }

        if (isHost)
            SetText(waitStatusText, canStart ? "Everyone ready? Press Start Game." : "Waiting for players... share the lobby code.");
        else
            SetText(waitStatusText, "Waiting for the host to start the game...");
    }

    private void Toast(string message)
    {
        if (toastText == null) return;
        toastText.text = message;
        toastText.gameObject.SetActive(true);
        toastText.transform.SetAsLastSibling();
        toastTimer = 5f;
    }
}
