using UnityEngine;
using Unity.Netcode;

public class PlayerStateManager : NetworkBehaviour
{
    private NetworkVariable<PlayerState> _currentState =
        new NetworkVariable<PlayerState>(
            PlayerState.Idle,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

    public PlayerState CurrentState => _currentState.Value;

    public override void OnNetworkSpawn()
    {
        _currentState.OnValueChanged += OnStateChanged;
        OnStateChanged(PlayerState.Idle, _currentState.Value);
    }

    public override void OnNetworkDespawn()
    {
        _currentState.OnValueChanged -= OnStateChanged;
    }

    public void SetState(PlayerState newState)
    {
        if (!IsServer) return;
        if (_currentState.Value == newState) return;
        _currentState.Value = newState;
    }

    [ServerRpc]
    public void RequestStateChangeServerRpc(PlayerState requestedState)
    {
        if (CanTransitionTo(requestedState))
        {
            _currentState.Value = requestedState;
        }
    }

    private bool CanTransitionTo(PlayerState requested)
    {
        PlayerState current = _currentState.Value;

        if (current == PlayerState.Captured) return false;
        if (current == PlayerState.Escaped && requested != PlayerState.Idle) return false;

        return true;
    }

    private void OnStateChanged(PlayerState oldState, PlayerState newState)
    {
        Debug.Log($"[Player {OwnerClientId}] {oldState} → {newState}");
    }
    public bool CanMove()
    {
        return CurrentState switch
        {
            PlayerState.Idle => true,
            PlayerState.Walking => true,
            PlayerState.Sprinting => true,
            PlayerState.Crouching => true,
            PlayerState.Climbing => true,
            PlayerState.Chased => true,
            PlayerState.Escaped => true,
            _ => false
        };
    }

    public bool CanBeDetected()
    {
        return CurrentState switch
        {
            PlayerState.Captured => false,
            PlayerState.Escaped => false,
            PlayerState.Chased => false,
            _ => true
        };
    }

    public bool IsLockedInPlace()
    {
        return CurrentState == PlayerState.Posing
            || CurrentState == PlayerState.Captured
            || CurrentState == PlayerState.Petrified;
    }

    public bool IsHidden()
    {
        return CurrentState == PlayerState.Posing;
    }
}