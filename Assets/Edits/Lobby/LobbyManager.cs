using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.VisualScripting;
// changed: removed "using UnityEditor.Rendering;" - editor-only namespace, it breaks player builds
using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyManager : MonoBehaviour {


    public static LobbyManager Instance { get; private set; }


    public const string KEY_PLAYER_NAME = "PlayerName";
    public const string KEY_PLAYER_CHARACTER = "Character";
    public const string KEY_GAME_MODE = "GameMode";
    public const string KEY_HOST_ROLE = "HostRole";
    public const string KEY_MAP = "Map";
    public const string KEY_RELAY_CODE = "RelayJoinCode";

    public const int MAX_PLAYERS = 5;



    public event EventHandler OnLeftLobby;

    public event EventHandler<LobbyEventArgs> OnJoinedLobby;
    public event EventHandler<LobbyEventArgs> OnJoinedLobbyUpdate;
    public event EventHandler<LobbyEventArgs> OnKickedFromLobby;
    public event EventHandler<LobbyEventArgs> OnLobbyGameModeChanged;

    public event EventHandler OnGameStarting;
    //For if there is errors it does not break the game.
    public event Action<string> OnLobbyError;
    
    public class LobbyEventArgs : EventArgs {
        public Lobby lobby;
    }

    public event EventHandler<OnLobbyListChangedEventArgs> OnLobbyListChanged;
    public class OnLobbyListChangedEventArgs : EventArgs {
        public List<Lobby> lobbyList;
    }


    public enum GameMode {
        LastManStanding,
        Escape
    }

    public enum PlayerCharacter {
        Marine,
        Ninja,
        Zombie
    }

    public enum PlayerRole
    {
       Human,
       Monster
    }


    private float heartbeatTimer;
    private float lobbyPollTimer;
    private float refreshLobbyListTimer = 5f;
    private Lobby joinedLobby;
    private string playerName;
    private bool hasGameStarted;


    private void Awake() {
        Instance = this;
    }

    private void Update() {
        //HandleRefreshLobbyList(); // Disabled Auto Refresh for testing with multiple builds
        HandleLobbyHeartbeat();
        HandleLobbyPolling();
    }

    public async void Authenticate(string playerName) {
        this.playerName = playerName;
        InitializationOptions initializationOptions = new InitializationOptions();
        initializationOptions.SetProfile(playerName);

        await UnityServices.InitializeAsync(initializationOptions);

        AuthenticationService.Instance.SignedIn += () => {
            // do nothing
            Debug.Log("Signed in! " + AuthenticationService.Instance.PlayerId);

            RefreshLobbyList();
        };

        await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }
    /// <summary>
    /// Task takes in a player name used in the Host and Join Lobby screen
    /// </summary>
    /// <param name="playerName">The name the player has chosen for themselves</param>
    /// <returns></returns>
    public async Task AuthenticateAsync(string playerName)
    {
        this.playerName = playerName;

        if(UnityServices.State != ServicesInitializationState.Initialized)
        {
            InitializationOptions options = new InitializationOptions();
            options.SetProfile(setPlayerName(playerName));
            await UnityServices.InitializeAsync(options);
        }

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            Debug.Log($"You have signed in {playerName}#{AuthenticationService.Instance.PlayerId}");
        }
    }

    private static string setPlayerName(string name)
    {
        string sanatizedName = "";
        foreach(char c in name ?? "")
        {
            if(c <128 && char.IsLetterOrDigit(c)) sanatizedName += c;
        }
        if(sanatizedName.Length > 20)
        {
            sanatizedName = sanatizedName.Substring(0, 20);
        }
        return sanatizedName + UnityEngine.Random.Range(100, 999);
    }

    private void HandleRefreshLobbyList() {
        if (UnityServices.State == ServicesInitializationState.Initialized && AuthenticationService.Instance.IsSignedIn) {
            refreshLobbyListTimer -= Time.deltaTime;
            if (refreshLobbyListTimer < 0f) {
                float refreshLobbyListTimerMax = 5f;
                refreshLobbyListTimer = refreshLobbyListTimerMax;

                RefreshLobbyList();
            }
        }
    }

    private async void HandleLobbyHeartbeat() {
        if (IsLobbyHost() && !hasGameStarted) {
            heartbeatTimer -= Time.deltaTime;
            if (heartbeatTimer < 0f) {
                float heartbeatTimerMax = 15f;
                heartbeatTimer = heartbeatTimerMax;

                Debug.Log("Heartbeat");
                // Added try catch methods due to the use of unity Relay
                try
                {
                    await LobbyService.Instance.SendHeartbeatPingAsync(joinedLobby.Id);
                }
                catch (Exception e)
                {
                    Debug.LogWarning(e);
                }
                
            }
        }
    }

    private async void HandleLobbyPolling() {
        if (joinedLobby != null && !hasGameStarted) {
            lobbyPollTimer -= Time.deltaTime;
            if (lobbyPollTimer < 0f) {
                float lobbyPollTimerMax = 1.1f;
                lobbyPollTimer = lobbyPollTimerMax;

                try
                {
                    Lobby newLobby = await LobbyService.Instance.GetLobbyAsync(joinedLobby.Id);
                    if (this == null || joinedLobby == null) return; // guard statement
                    joinedLobby = newLobby;
                }
                catch (LobbyServiceException e)
                {
                    Debug.LogWarning(e);
                    // Removes the player from the lobby if the lobby is not found and sets the joined lobby to null as to not throw errors elsewhere
                    if (e.Reason == LobbyExceptionReason.LobbyNotFound && joinedLobby != null)
                    {
                        Lobby closedLobby = joinedLobby;
                        joinedLobby = null;
                        OnKickedFromLobby?.Invoke(this, new LobbyEventArgs { lobby = closedLobby }); // Removes the player from the lobby
                    }
                    return;   // changed: nothing more to do this tick (joinedLobby may be null now)
                }
                if (joinedLobby == null) return;   // changed: guard

                // added: tell the waiting room the lobby changed (players joined / left, map, game mode)
                OnJoinedLobbyUpdate?.Invoke(this, new LobbyEventArgs { lobby = joinedLobby });

                // changed: the old check "!IsLobbyHost()" kicked every client on the first poll. A player is only kicked when they are really gone from the lobby.
                if (!IsPlayerInLobby())
                {
                    Debug.Log("Kicked from Lobby!");
                    Lobby kickedFrom = joinedLobby;
                    joinedLobby = null;
                    OnKickedFromLobby?.Invoke(this, new LobbyEventArgs { lobby = kickedFrom });
                    return;
                }

                // changed: clients follow the host into the game once the Relay join code is published (was an empty "StartClientGame" stub)
                if (!IsLobbyHost() && joinedLobby.Data != null && joinedLobby.Data.TryGetValue(KEY_RELAY_CODE, out DataObject relayData) && !string.IsNullOrEmpty(relayData.Value))
                {
                    ClientStartGame(relayData.Value);
                }
            }
        }
    }

    public Lobby GetJoinedLobby() {
        return joinedLobby;
    }

    public bool IsLobbyHost() {
        return joinedLobby != null && joinedLobby.HostId == AuthenticationService.Instance.PlayerId;
    }

    private bool IsPlayerInLobby() {
        if (joinedLobby != null && joinedLobby.Players != null) {
            foreach (Player player in joinedLobby.Players) {
                if (player.Id == AuthenticationService.Instance.PlayerId) {
                    // This player is in this lobby
                    return true;
                }
            }
        }
        return false;
    }

    private Player GetPlayer() {
        return new Player(AuthenticationService.Instance.PlayerId, null, new Dictionary<string, PlayerDataObject> {
            { KEY_PLAYER_NAME, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, playerName) },
            { KEY_PLAYER_CHARACTER, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, PlayerCharacter.Marine.ToString()) }
        });
    }
    /*
    public void ChangeGameMode() {
        if (IsLobbyHost()) {
            GameMode gameMode =
                Enum.Parse<GameMode>(joinedLobby.Data[KEY_GAME_MODE].Value);

            switch (gameMode) {
                default:
                case GameMode.CaptureTheFlag:
                    gameMode = GameMode.Conquest;
                    break;
                case GameMode.Conquest:
                    gameMode = GameMode.CaptureTheFlag;
                    break;
            }

            UpdateLobbyGameMode(gameMode);
        }
    }
    */
    #region Player Role Management
    public static PlayerRole GetHostRole(Lobby hostLobby)
    {
        if(hostLobby != null && hostLobby.Data.TryGetValue(KEY_HOST_ROLE, out DataObject data) && data.Value == PlayerRole.Monster.ToString())
        {
            return PlayerRole.Monster;
        }
        return PlayerRole.Human;
    }
    public static PlayerRole getPlayerRole(Lobby lobby, string playerId)
    {
        return getMonsterId(lobby) == playerId ? PlayerRole.Monster : PlayerRole.Human;
    }
    public PlayerRole getMyRole()
    {
        return getPlayerRole(joinedLobby, AuthenticationService.Instance.PlayerId);
    }
    #endregion Player Role Management

    #region Lobby Setup Getters
    public static string getMonsterId(Lobby currentLobby)
    {
        if (currentLobby == null || currentLobby.Players == null) return null;
        if(GetHostRole(currentLobby) == PlayerRole.Monster) return currentLobby.HostId;

        Player firstJoinedPlayer = null; // needs to be set to something otherwise it will throw an error
        foreach(Player players in currentLobby.Players)
        {
            if (players.Id == currentLobby.HostId) continue;
            if(firstJoinedPlayer == null || players.Joined < firstJoinedPlayer.Joined) firstJoinedPlayer = players;
        }
        return firstJoinedPlayer != null ? firstJoinedPlayer.Id : null;
    }
    public static string getMap(Lobby lobby)
    {
        if(lobby != null && lobby.Data != null && lobby.Data.TryGetValue(KEY_MAP, out DataObject data) && !string.IsNullOrEmpty(data.Value))
        {
            return data.Value;
        }
        return null; // Overwrite to check all the scenes and maps available
    }

    public static GameMode GetGameMode(Lobby lobby)
    {
        if (lobby != null && lobby.Data != null && lobby.Data.TryGetValue(KEY_GAME_MODE, out DataObject data) && Enum.TryParse(data.Value, out GameMode gamemode))
        {
            return gamemode;
        }
        return GameMode.LastManStanding;
    }
    #endregion Lobby setup Getters

    public static string setGameModeDisplay(GameMode gamemode)
    {
        switch(gamemode)
        {
            case GameMode.Escape: return "Escape";
            default: return "Last Man Standing";
        }
    }
    /// <summary>
    /// Checks if the player is the host and whether there is more than one player in the lobby
    /// </summary>
    /// <returns></returns>
    public bool canHostStartGame()
    {
        return IsLobbyHost() && joinedLobby != null && joinedLobby.Players.Count > 1;
    }
    private static string defaultErrorMessages(Exception e)
    {
        if(e is LobbyServiceException lobbyServiceException)
        {
            switch (lobbyServiceException.Reason)
            {
                case LobbyExceptionReason.LobbyFull: return ($"The Lobby is Full! \n Max Players:{MAX_PLAYERS}");
                    case LobbyExceptionReason.LobbyNotFound: return ($"There is lobby with that join code");
                default: return lobbyServiceException.Message;
            }
        }
        return e.Message;
    }
    public async Task<bool> CreateLobbyAsync(string hostName, PlayerRole hostRole, string mapName)
    {
        try
        {
            await AuthenticateAsync(hostName);

            CreateLobbyOptions options = new CreateLobbyOptions
            {
                Player = GetPlayer(),
                IsPrivate = false,
                Data = new Dictionary<string, DataObject>
                {
                    {KEY_GAME_MODE, new DataObject(DataObject.VisibilityOptions.Public, GameMode.LastManStanding.ToString()) },
                    {KEY_HOST_ROLE, new DataObject(DataObject.VisibilityOptions.Public, hostRole.ToString()) },
                    {KEY_MAP, new DataObject(DataObject.VisibilityOptions.Public, mapName)}
                }
            };

            Lobby currentLobby = await LobbyService.Instance.CreateLobbyAsync($"{hostName}'s Lobby", MAX_PLAYERS, options);

            hasGameStarted = false;
            joinedLobby = currentLobby;
            lobbyPollTimer = 1.2f;
            OnJoinedLobby?.Invoke(this, new LobbyEventArgs { lobby = currentLobby });
            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning(e);
            OnLobbyError?.Invoke(defaultErrorMessages(e));   // changed: null-safe (NullReferenceException when nobody listens)
            return false;
        }
    }

    public async Task<bool> JoinLobbyByCodeAsync(string nameOfJoinedPlayer, string lobbyCode)
    {
        try
        {
            await AuthenticateAsync(nameOfJoinedPlayer);
            Lobby lobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode.Trim().ToUpperInvariant(), new JoinLobbyByCodeOptions
            {
                Player = GetPlayer()
            });

            hasGameStarted = false;
            joinedLobby = lobby;
            lobbyPollTimer = 1.2f;
            OnJoinedLobby?.Invoke(this, new LobbyEventArgs { lobby = lobby });
            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning(e);
            OnLobbyError?.Invoke(defaultErrorMessages(e));   // changed: null-safe (NullReferenceException when nobody listens)
            return false;
        }
    }

    public async void changeMap(string mapName)
    {
        if (!IsLobbyHost()) return;
        try
        {
            joinedLobby = await LobbyService.Instance.UpdateLobbyAsync(joinedLobby.Id, new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
                {
                    { KEY_MAP, new DataObject(DataObject.VisibilityOptions.Public, mapName) }
                }
                });
            OnJoinedLobbyUpdate?.Invoke(this, new LobbyEventArgs { lobby = joinedLobby });
        }
        catch(Exception e)
        {
            Debug.LogWarning(e);
            OnLobbyError?.Invoke(defaultErrorMessages(e));
        }
    }

    public async void StartGame()
    {
        if(!canHostStartGame() || hasGameStarted) return;
        hasGameStarted = true;
        try
        {
            Unity.Services.Relay.Models.Allocation allocation = await Unity.Services.Relay.RelayService.Instance.CreateAllocationAsync(MAX_PLAYERS - 1);
            string joinCode = await Unity.Services.Relay.RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            string map = MapList.ResolveLoadable(getMap(joinedLobby));

            joinedLobby = await LobbyService.Instance.UpdateLobbyAsync(joinedLobby.Id, new UpdateLobbyOptions
            {
                IsLocked = true,
                Data = new Dictionary<string , DataObject>
                {
                    {KEY_MAP, new DataObject(DataObject.VisibilityOptions.Public, map) },
                    {KEY_RELAY_CODE, new DataObject(DataObject.VisibilityOptions.Member, joinCode) } 
                }
            });


            LobbySession.isActive = true;
            LobbySession.isHost = true;
            LobbySession.role = getMyRole();
            LobbySession.relayJoinCode = joinCode;
            LobbySession.hostAllocation = allocation;
            LobbySession.mapName = map;
            LobbySession.playerName = playerName;
            LobbySession.gameMode = GetGameMode(joinedLobby);

            OnGameStarting?.Invoke(this, EventArgs.Empty);
            SceneManager.LoadScene(map);
        }
        catch(Exception e)
        {
            hasGameStarted = false;
            Debug.LogError(e.ToString());
            OnLobbyError?.Invoke(defaultErrorMessages(e));
        }
    }

    private void ClientStartGame(string relayJoinCode)
    {
        if (hasGameStarted) return;
        hasGameStarted = true;
        string map = MapList.ResolveLoadable(getMap(joinedLobby));

        LobbySession.isActive = true;
        LobbySession.isHost = false;   // changed: this is the client path (was true, so clients tried to host)
        LobbySession.role = getMyRole();
        LobbySession.relayJoinCode = relayJoinCode;
        LobbySession.hostAllocation = null;
        LobbySession.mapName = map;
        LobbySession.playerName = playerName;
        LobbySession.gameMode = GetGameMode(joinedLobby);

        OnGameStarting?.Invoke(this, EventArgs.Empty);
        SceneManager.LoadScene(map);
    }

    public void ChangeGamemode()
    {
        if (IsLobbyHost())
        {
            GameMode mode = Enum.Parse<GameMode>(joinedLobby.Data[KEY_GAME_MODE].Value);

            switch (mode)
            {
                default:
                case GameMode.LastManStanding:
                    mode = GameMode.Escape;
                    break;
                case GameMode.Escape:
                    mode = GameMode.LastManStanding;
                    break;
            }
            UpdateLobbyGameMode(mode);
        }
        
    }

    public async void CreateLobby(string lobbyName, int maxPlayers, bool isPrivate, GameMode gameMode) {
        Player player = GetPlayer();
        maxPlayers = Mathf.Clamp(maxPlayers, 2, MAX_PLAYERS);
        CreateLobbyOptions options = new CreateLobbyOptions {
            Player = player,
            IsPrivate = isPrivate,
            Data = new Dictionary<string, DataObject> {
                { KEY_GAME_MODE, new DataObject(DataObject.VisibilityOptions.Public, gameMode.ToString()) }
            }
        };

        Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);

        joinedLobby = lobby;

        OnJoinedLobby?.Invoke(this, new LobbyEventArgs { lobby = lobby });

        Debug.Log("Created Lobby " + lobby.Name);
    }

    public async void RefreshLobbyList() {
        try {
            QueryLobbiesOptions options = new QueryLobbiesOptions();
            options.Count = 25;

            // Filter for open lobbies only
            options.Filters = new List<QueryFilter> {
                new QueryFilter(
                    field: QueryFilter.FieldOptions.AvailableSlots,
                    op: QueryFilter.OpOptions.GT,
                    value: "0")
            };

            // Order by newest lobbies first
            options.Order = new List<QueryOrder> {
                new QueryOrder(
                    asc: false,
                    field: QueryOrder.FieldOptions.Created)
            };

            QueryResponse lobbyListQueryResponse = await LobbyService.Instance.QueryLobbiesAsync();

            OnLobbyListChanged?.Invoke(this, new OnLobbyListChangedEventArgs { lobbyList = lobbyListQueryResponse.Results });
        } catch (LobbyServiceException e) {
            Debug.Log(e);
        }
    }

    public async void JoinLobbyByCode(string lobbyCode) {
        Player player = GetPlayer();

        Lobby lobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode, new JoinLobbyByCodeOptions {
            Player = player
        });

        joinedLobby = lobby;

        OnJoinedLobby?.Invoke(this, new LobbyEventArgs { lobby = lobby });
    }

    public async void JoinLobby(Lobby lobby) {
        Player player = GetPlayer();

        joinedLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobby.Id, new JoinLobbyByIdOptions {
            Player = player
        });

        OnJoinedLobby?.Invoke(this, new LobbyEventArgs { lobby = lobby });
    }

    public async void UpdatePlayerName(string playerName) {
        this.playerName = playerName;

        if (joinedLobby != null) {
            try {
                UpdatePlayerOptions options = new UpdatePlayerOptions();

                options.Data = new Dictionary<string, PlayerDataObject>() {
                    {
                        KEY_PLAYER_NAME, new PlayerDataObject(
                            visibility: PlayerDataObject.VisibilityOptions.Public,
                            value: playerName)
                    }
                };

                string playerId = AuthenticationService.Instance.PlayerId;

                Lobby lobby = await LobbyService.Instance.UpdatePlayerAsync(joinedLobby.Id, playerId, options);
                joinedLobby = lobby;

                OnJoinedLobbyUpdate?.Invoke(this, new LobbyEventArgs { lobby = joinedLobby });
            } catch (LobbyServiceException e) {
                Debug.Log(e);
            }
        }
    }

    public async void UpdatePlayerCharacter(PlayerCharacter playerCharacter) {
        if (joinedLobby != null) {
            try {
                UpdatePlayerOptions options = new UpdatePlayerOptions();

                options.Data = new Dictionary<string, PlayerDataObject>() {
                    {
                        KEY_PLAYER_CHARACTER, new PlayerDataObject(
                            visibility: PlayerDataObject.VisibilityOptions.Public,
                            value: playerCharacter.ToString())
                    }
                };

                string playerId = AuthenticationService.Instance.PlayerId;

                Lobby lobby = await LobbyService.Instance.UpdatePlayerAsync(joinedLobby.Id, playerId, options);
                joinedLobby = lobby;

                OnJoinedLobbyUpdate?.Invoke(this, new LobbyEventArgs { lobby = joinedLobby });
            } catch (LobbyServiceException e) {
                Debug.Log(e);
            }
        }
    }

    public async void QuickJoinLobby() {
        try {
            QuickJoinLobbyOptions options = new QuickJoinLobbyOptions();

            Lobby lobby = await LobbyService.Instance.QuickJoinLobbyAsync(options);
            joinedLobby = lobby;

            OnJoinedLobby?.Invoke(this, new LobbyEventArgs { lobby = lobby });
        } catch (LobbyServiceException e) {
            Debug.Log(e);
        }
    }

    public async void LeaveLobby() {
        if (joinedLobby != null) {
            try 
            {
                if (IsLobbyHost())
                {
                    await LobbyService.Instance.DeleteLobbyAsync(joinedLobby.Id);
                }
                else
                {
                    await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, AuthenticationService.Instance.PlayerId);
                }
            } 
            
            catch (LobbyServiceException e) 
            {
                Debug.Log(e);
            }
            joinedLobby = null;
            OnLeftLobby?.Invoke(this, EventArgs.Empty);
        }
    }

    public async void KickPlayer(string playerId) {
        if (IsLobbyHost()) {
            try {
                await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, playerId);
            } catch (LobbyServiceException e) {
                Debug.Log(e);
            }
        }
    }

    public async void UpdateLobbyGameMode(GameMode gamemode) {
        try {
            Debug.Log("UpdateLobbyGameMode " + gamemode);
            
            Lobby lobby = await LobbyService.Instance.UpdateLobbyAsync(joinedLobby.Id, new UpdateLobbyOptions {
                Data = new Dictionary<string, DataObject> {
                    { KEY_GAME_MODE, new DataObject(DataObject.VisibilityOptions.Public, gamemode.ToString()) }
                }
            });

            joinedLobby = lobby;

            OnLobbyGameModeChanged?.Invoke(this, new LobbyEventArgs { lobby = joinedLobby });
        } catch (LobbyServiceException e) {
            Debug.Log(e);
        }
    }

}
