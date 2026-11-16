// ADDED SCRIPT - new file (Assets/Edits/Lobby/Additions/LobbyGameBootstrap.cs)
// Runs automatically (no scene setup needed). When a game scene loads after the host pressed Start Game it
// connects Netcode through Unity Relay: the host calls StartHost, everybody else calls StartClient.
// Uses this project's LobbySession fields (isActive, isHost, relayJoinCode, hostAllocation).
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class LobbyGameBootstrap   // added
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Register()        // added
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)   // added
    {
        if (!LobbySession.isActive) return;
        if (scene.name == "MainMenu") return;            // the menu never starts networking
        ConnectAsync();
    }

    private static async void ConnectAsync()   // added
    {
        // the NetworkManager lives in the game scene, give it a few frames to exist
        int frames = 0;
        while (NetworkManager.Singleton == null && frames < 300)
        {
            await Task.Yield();
            frames++;
        }

        NetworkManager manager = NetworkManager.Singleton;
        if (manager == null)
        {
            Debug.LogError("LobbyGameBootstrap: no NetworkManager found in the game scene.");
            return;
        }
        if (manager.IsListening) return;                  // already started (e.g. by hand)

        UnityTransport transport = manager.GetComponent<UnityTransport>();
        if (transport == null)
        {
            Debug.LogError("LobbyGameBootstrap: the NetworkManager has no UnityTransport.");
            return;
        }

        try
        {
            if (LobbySession.isHost)
            {
                Unity.Services.Relay.Models.Allocation a = LobbySession.hostAllocation;
                string host = null;
                int port = 0;
                bool secure = false;
                foreach (var e in a.ServerEndpoints)
                {
                    if (e.ConnectionType == "dtls") { host = e.Host; port = e.Port; secure = e.Secure; break; }
                }
                if (host == null) { var e0 = a.ServerEndpoints[0]; host = e0.Host; port = e0.Port; secure = e0.Secure; }

                transport.SetRelayServerData(host, (ushort)port, a.AllocationIdBytes, a.Key, a.ConnectionData, null, secure);
                manager.StartHost();
            }
            else
            {
                Unity.Services.Relay.Models.JoinAllocation a =
                    await Unity.Services.Relay.RelayService.Instance.JoinAllocationAsync(LobbySession.relayJoinCode);
                string host = null;
                int port = 0;
                bool secure = false;
                foreach (var e in a.ServerEndpoints)
                {
                    if (e.ConnectionType == "dtls") { host = e.Host; port = e.Port; secure = e.Secure; break; }
                }
                if (host == null) { var e0 = a.ServerEndpoints[0]; host = e0.Host; port = e0.Port; secure = e0.Secure; }

                transport.SetRelayServerData(host, (ushort)port, a.AllocationIdBytes, a.Key, a.ConnectionData, a.HostConnectionData, secure);
                manager.StartClient();
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("LobbyGameBootstrap: could not start networking - " + ex);
        }
    }
}
