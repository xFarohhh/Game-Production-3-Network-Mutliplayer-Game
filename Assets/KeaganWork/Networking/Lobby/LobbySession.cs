using Unity.Services.Relay.Models;

public static class LobbySession
{
    public static bool isActive;
    public static bool isHost;
    public static LobbyManager.PlayerRole role;
    public static string relayJoinCode;
    public static Allocation hostAllocation;
    public static string mapName;
    public static string playerName;
    public static LobbyManager.GameMode gameMode;

    public static void ClearLobby()
    {
        isActive = false;
        isHost = false;
        relayJoinCode = null;
        hostAllocation = null;
        mapName = null;
        gameMode = LobbyManager.GameMode.LastManStanding;
    }
}
