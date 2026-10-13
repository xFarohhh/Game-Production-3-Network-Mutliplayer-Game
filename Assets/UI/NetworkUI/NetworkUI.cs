using UnityEngine;
using Unity.Netcode;

public class NetworkUI : MonoBehaviour
{
    private void OnGUI()
    {
        // Guard: If the NetworkManager isn't ready yet, do nothing
        if (NetworkManager.Singleton == null) return;

        GUILayout.BeginArea(new Rect(10, 10, 300, 300));

        if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
        {
            if (GUILayout.Button("Start Host"))
                NetworkManager.Singleton.StartHost();

            if (GUILayout.Button("Start Client"))
                NetworkManager.Singleton.StartClient();
        }
        else
        {
            string mode = NetworkManager.Singleton.IsHost ? "Host" :
                          NetworkManager.Singleton.IsServer ? "Server" : "Client";
            GUILayout.Label($"Mode: {mode}");
        }

        GUILayout.EndArea();
    }
}