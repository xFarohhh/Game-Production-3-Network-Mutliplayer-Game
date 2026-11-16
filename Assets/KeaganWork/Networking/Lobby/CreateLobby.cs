using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CreateLobby : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] Button createLobbyButton;
    [SerializeField] Button nameLobbyButton;
    [SerializeField] Button isLobbyPrivateButton;
    [SerializeField] Button chooseGameModeButton;
    [SerializeField] Button maxPlayersInLobbyButton;

    [Header("Text")]
    [SerializeField] TextMeshProUGUI lobbyNameText;
    [SerializeField] TextMeshProUGUI isLobbyPrivateText;
    [SerializeField] TextMeshProUGUI maxPlayersText;
    [SerializeField] TextMeshProUGUI gamemodeText;

    [Header("Create Lobby Variables")]
    [Tooltip("Variables used for host when they are creating the lobby so they may be visible and interacted with through the text and buttons from both host and user end")]
    private string currentLobbyName;
    private bool isLobbyPrivate;
    private int maxPlayers;
    private LobbyManager.GameMode gamemode;

    public static CreateLobby instance;

    private void Awake()
    {
        instance = this;

        // assigns the Buttons the appropriate functions

        //  Used to create the lobby or Host the lobby
        #region Create Lobby Button
        createLobbyButton.onClick.AddListener(() =>
        {
            LobbyManager.Instance.CreateLobby(currentLobbyName, maxPlayers, isLobbyPrivate, gamemode);

        });
        #endregion Create Lobby Button

        //used to assign the player a username  because the lobby is created by using the users name followed by Lobby e.g "Bob's Lobby"
        #region Choose Lobby Name Button

        nameLobbyButton.onClick.AddListener(() =>
        {
            UI_InputWindow.Show_Static("Lobby Name", currentLobbyName, "abcdefghijklmnopqrstuvxywzABCDEFGHIJKLMNOPQRSTUVXYWZ .,-", 20,
            () =>
            {
                // Cancel Button press
            },
            (string lobbyName) =>
            {
                this.currentLobbyName = lobbyName;
                updateUIText();
            });
        });
        #endregion Choose Lobby Name Button

        //Controls whether the lobby is private or not when created
        #region Is Lobby Private Button
        isLobbyPrivateButton.onClick.AddListener(() =>
        {
            isLobbyPrivate = !isLobbyPrivate;
            updateUIText();
        });
        #endregion Is Lobby Private Button

        // This function is used to choose how many players is allowed in a lobby at a time.
        #region Max Players Button
        maxPlayersInLobbyButton.onClick.AddListener(() =>
        {
            UI_InputWindow.Show_Static("Max Players", maxPlayers,
                () =>
                {
                    //Cancel
                },
                (int maxPlayersInLobby) =>
                {
                    this.maxPlayers = maxPlayersInLobby;
                    updateUIText();

                });
        });
        #endregion Max Players Button

        #region Choose Gamemode Button
        // Choose game Mode
        #endregion Choose Gamemode Button
    }

    private void updateUIText()
    {
        lobbyNameText.text = currentLobbyName;
        isLobbyPrivateText.text = isLobbyPrivate ? "Private" : "Public";
        maxPlayersText.text = maxPlayers.ToString();
        gamemodeText.text = gamemode.ToString();
    }

    private void hideSelf()
    {
        gameObject.SetActive(false);
    }
    private void showSelf()
    {
        gameObject.SetActive(true);

        currentLobbyName = "My Lobby";
        isLobbyPrivate = false;
        maxPlayers = 5;
        //gamemode = LobbyManager.GameMode

        updateUIText();
    }


}
