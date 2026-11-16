// ADDED SCRIPT - new file (Assets/Edits/Lobby/Additions/PlayerSpawnSetup.cs)
// Two small runtime helpers for players spawned by RoleSpawnApproval:
//  - PlayerSpawnWatcher finds every spawned player object and gives it a PlayerSpawnSetup.
//  - PlayerSpawnSetup makes only the OWNER's copy a real, controllable player: camera, audio listener, input and
//    (for the Monster) MonsterLocomotion are off on every other copy. A Monster prefab without a CharacterController gets one.
using System.Collections.Generic;
using System.Reflection;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerSpawnWatcher : MonoBehaviour   // added
{
    private static PlayerSpawnWatcher instance;
    private float timer;

    public static void Ensure()   // added
    {
        if (instance != null) return;
        GameObject go = new GameObject("PlayerSpawnWatcher");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<PlayerSpawnWatcher>();
    }

    private void Update()
    {
        timer -= Time.unscaledDeltaTime;
        if (timer > 0f) return;
        timer = 0.2f;

        NetworkManager nm = NetworkManager.Singleton;
        if (nm == null || !nm.IsListening || nm.SpawnManager == null) return;

        List<NetworkObject> spawned = new List<NetworkObject>(nm.SpawnManager.SpawnedObjectsList);
        foreach (NetworkObject no in spawned)
        {
            if (no == null || !no.IsPlayerObject) continue;
            if (no.GetComponent<PlayerSpawnSetup>() == null) no.gameObject.AddComponent<PlayerSpawnSetup>();
        }
    }
}

public class PlayerSpawnSetup : MonoBehaviour   // added
{
    public bool IsMonster { get; private set; }

    private void Start()
    {
        NetworkObject no = GetComponent<NetworkObject>();
        bool owner = no != null && no.IsOwner;
        MonsterLocomotion monsterLoco = GetComponent<MonsterLocomotion>();
        IsMonster = monsterLoco != null;

        // only the owner looks through this player's camera and hears through it
        foreach (Camera c in GetComponentsInChildren<Camera>(true)) c.enabled = owner;
        foreach (AudioListener a in GetComponentsInChildren<AudioListener>(true)) a.enabled = owner;

        PlayerInput input = GetComponent<PlayerInput>();
        if (input != null) input.enabled = owner;

        if (IsMonster)
        {
            EnsureCharacterController(monsterLoco);
            monsterLoco.enabled = owner;   // the Monster is moved by its owner (ClientNetworkTransform syncs it)
        }

        Debug.Log("PlayerSpawnSetup: " + name + " (client " + (no != null ? no.OwnerClientId : 0) + ") spawned as " +
                  (IsMonster ? "Monster" : "Human") + (owner ? " [mine]" : ""));
    }

    // MonsterLocomotion calls CharacterController.Move every frame but the Monster prefab had no CharacterController
    private void EnsureCharacterController(MonsterLocomotion loco)
    {
        CharacterController cc = GetComponent<CharacterController>();
        if (cc == null)
        {
            cc = gameObject.AddComponent<CharacterController>();

            Bounds bounds = new Bounds(transform.position + Vector3.up, Vector3.zero);
            bool first = true;
            foreach (Renderer r in GetComponentsInChildren<Renderer>(true))
            {
                if (first) { bounds = r.bounds; first = false; }
                else bounds.Encapsulate(r.bounds);
            }
            cc.height = Mathf.Max(1.8f, bounds.size.y);
            cc.radius = 0.5f;
            cc.stepOffset = 0.3f;
            cc.center = transform.InverseTransformPoint(new Vector3(bounds.center.x, bounds.center.y, bounds.center.z));
        }

        // MonsterLocomotion read GetComponent<CharacterController>() in Awake, before it existed - hand it over
        FieldInfo field = typeof(MonsterLocomotion).GetField("_characterController", BindingFlags.NonPublic | BindingFlags.Instance);
        if (field != null && field.GetValue(loco) == null) field.SetValue(loco, cc);
    }
}
