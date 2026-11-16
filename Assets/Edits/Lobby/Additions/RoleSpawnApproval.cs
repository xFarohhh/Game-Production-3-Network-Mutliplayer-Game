// ADDED SCRIPT - new file (Assets/Edits/Lobby/Additions/RoleSpawnApproval.cs)
// Spawns every player with the prefab of their lobby role: Monster -> "Monster Prefab", Human -> "PlayerPrefab".
// How: before the host / client starts (called from LobbyGameBootstrap) every peer turns on Netcode connection approval and sends
// its lobby role as the connection data. The host answers each connection request with the matching player prefab (and a spawn point
// when the map has objects named like "spawn"). Only one Monster is allowed - a second Monster request is downgraded to Human.
// The two prefabs only have to be in the NetworkManager's network prefab list (they are, in DefaultNetworkPrefabs).
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public static class RoleSpawnApproval   // added
{
    public const string HumanPrefabName = "PlayerPrefab";     // added: name of the human player prefab (Assets/Prefabs/Player/PlayerPrefab.prefab)
    public const string MonsterPrefabName = "Monster Prefab"; // added: name of the monster prefab (Assets/Prefabs/Player/Monster Prefab.prefab)

    private static uint? humanHash, monsterHash;
    private static bool monsterAssigned;
    private static int spawnIndex;
    private static List<Transform> spawnPoints;

    // added: call right before StartHost / StartClient
    public static void Prepare(NetworkManager nm)
    {
        humanHash = null;
        monsterHash = null;
        monsterAssigned = false;
        spawnIndex = 0;
        spawnPoints = null;

        nm.NetworkConfig.ConnectionApproval = true;
        nm.NetworkConfig.ConnectionData = new byte[] { (byte)LobbySession.role };   // the host also approves itself with this
        nm.ConnectionApprovalCallback = Approve;                                    // only the host / server runs it
        PlayerSpawnWatcher.Ensure();
    }

    // added: host side - decides which prefab a connecting player gets
    private static void Approve(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        NetworkManager nm = NetworkManager.Singleton;

        bool wantsMonster = request.Payload != null && request.Payload.Length > 0
                            && request.Payload[0] == (byte)LobbyManager.PlayerRole.Monster;
        if (wantsMonster && monsterAssigned)
        {
            Debug.LogWarning("RoleSpawnApproval: a second Monster was requested, spawning that player as a Human instead.");
            wantsMonster = false;
        }

        uint? hash = wantsMonster ? FindHash(nm, MonsterPrefabName, ref monsterHash) : FindHash(nm, HumanPrefabName, ref humanHash);
        if (hash == null)
        {
            Debug.LogError("RoleSpawnApproval: the prefab \"" + (wantsMonster ? MonsterPrefabName : HumanPrefabName) +
                           "\" is not in the NetworkManager's network prefab list - falling back to the default player prefab.");
        }
        else if (wantsMonster)
        {
            monsterAssigned = true;
        }

        response.Approved = true;
        response.CreatePlayerObject = true;
        response.Pending = false;
        response.PlayerPrefabHash = hash;     // null = the NetworkManager's default PlayerPrefab

        Transform spawn = NextSpawnPoint();
        if (spawn != null)
        {
            response.Position = spawn.position;
            response.Rotation = spawn.rotation;
        }

        Debug.Log("RoleSpawnApproval: client " + request.ClientNetworkId + " -> " + (wantsMonster ? MonsterPrefabName : HumanPrefabName));
    }

    private static uint? FindHash(NetworkManager nm, string prefabName, ref uint? cache)
    {
        if (cache != null) return cache;
        foreach (NetworkPrefab np in nm.NetworkConfig.Prefabs.Prefabs)
        {
            if (np.Prefab != null && np.Prefab.name == prefabName)
            {
                cache = np.SourcePrefabGlobalObjectIdHash;
                return cache;
            }
        }
        return null;
    }

    // added: objects in the map whose name contains "spawn" are used as spawn points (round robin)
    private static Transform NextSpawnPoint()
    {
        if (spawnPoints == null)
        {
            spawnPoints = new List<Transform>();
            foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
            {
                if (t.GetComponent<NetworkObject>() == null && t.name.ToLowerInvariant().Contains("spawn")) spawnPoints.Add(t);
            }
        }
        if (spawnPoints.Count == 0) return null;
        return spawnPoints[spawnIndex++ % spawnPoints.Count];
    }
}
