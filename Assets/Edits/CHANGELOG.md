# Edits - change log (Game-Production-3-Network-Mutliplayer-Game)

Edited scripts were MOVED into `Assets/Edits` (their .meta files came along, so scene/prefab references still work).
Originals are in `Assets/Edits/Originals_Backup/` (.bak so Unity ignores them). Changes inside the code are marked `// changed` / `// added`.
Nothing was compiled or run in Unity from here - check the Console after the reload.

## Null-reference errors from the Unity log (all four fixed)
| Error | Fix |
|---|---|
| `MainMenuButtons.Awake` - CreateLobbyScreen not assigned | `Edits/UIButtons/MainMenuButtons.cs`: null checks everywhere |
| `LobbyListUI.Awake` line 26 | `Edits/Lobby/LobbyListUI.cs`: old tutorial screen disables itself (`enabled = false; return;`) |
| `LobbyUI.Awake` - playerSingleTemplate not assigned | `Edits/Lobby/LobbyUI.cs`: same |
| `LobbyCreateUI.Awake` line 42 | `Edits/Lobby/LobbyCreateUI.cs`: same |

## Lobby / Relay
| File | Action |
|---|---|
| `Lobby/LobbyManager.cs` | moved + edited. Removed the editor-only `using UnityEditor.Rendering` (breaks builds). Polling: the old `!IsLobbyHost()` test kicked every client on the first poll - now a player is kicked only when no longer in the lobby; the waiting room gets `OnJoinedLobbyUpdate` each poll; clients start the game when the Relay code appears (the `StartClientGame` stub was empty, now calls `ClientStartGame`); `ClientStartGame` set `isHost = true` for clients (now false); `OnLobbyError.Invoke` made null-safe. |
| `Lobby/RoleManager.cs` | moved + edited. The four Netcode message names were all `"_Role"` (each handler overwrote the previous), now `LB_Role/LB_Table/LB_Event/LB_Result`. |
| `Lobby/Additions/LobbyGameBootstrap.cs` | **added**. Nothing in the project connected Netcode through Relay. When a game scene loads with `LobbySession.isActive`, the host calls `SetRelayServerData` + `StartHost`, clients join with `LobbySession.relayJoinCode` + `StartClient`. |
| `Lobby/Additions/LobbyMenuUI.cs` | **added**. Host setup (username + Human/Monster), join (username + code), waiting room (players, roles, map, game mode, Start Game when 2+ players). Uses this project's names (`LobbySession.isActive`, `MapList`, `getMap`, ...). |
| `UIButtons/MainMenuButtons.cs` | Host Game / Join Lobby open the new screens; added `inputLobbyCodeButton`, `startGameButton`, `backToMainMenuButton`. |

## Role-based spawning (Monster Prefab / PlayerPrefab)
| File | Action |
|---|---|
| `Lobby/Additions/RoleSpawnApproval.cs` | **added**. Netcode connection approval: every peer sends its lobby role as connection data, the host spawns that player with `Monster Prefab` or `PlayerPrefab` (looked up by name in the NetworkManager's prefab list, so no asset references), uses objects named like "spawn" as spawn points, and allows only one Monster. |
| `Lobby/Additions/PlayerSpawnSetup.cs` | **added**. Owner-only camera / audio listener / input for every spawned player; the Monster gets `MonsterLocomotion` only on its owner and a `CharacterController` if its prefab has none. |
| `Lobby/Additions/LobbyGameBootstrap.cs` | changed: calls `RoleSpawnApproval.Prepare` before StartHost / StartClient. |
Both prefabs are already in `DefaultNetworkPrefabs.asset`. The prefabs themselves were NOT edited.

## Scene (`Scenes/MainMenu.unity`, backup `MainMenu.unity.bak` and `MainMenu.before_lobbyui.unity.bak`)
Added `Canvas/LobbyMenuUI` (67 objects) with inactive `HostSetupScreen`, `JoinLobbyScreen`, `WaitingRoomScreen` and `Toast Text`,
all references assigned to the `LobbyMenuUI` component. The old objects were left untouched. The scene already had a `LobbyManager` object.

## Already fine in this project (checked, not changed)
Tags `Crosshair` / `PauseScreen` / `settingsScreen`, layer `Player`, cloud project link (`ProjectSettings.asset`), NetworkManager + player prefab in `SampleScene`, netcode + services packages.

## Notes
- Lobby and Relay must be enabled in the Unity Dashboard for the linked cloud project.
- `MapList` only lists Abandoned Asylum (`SampleScene`); add more entries when more maps are in Build Settings.
- Not touched: `PlayerPauseHandler` (still the original) and `RoleManager`'s other logic.
