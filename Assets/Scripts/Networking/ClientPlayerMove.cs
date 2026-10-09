using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
public class ClientPlayerMove : NetworkBehaviour
{
    [SerializeField] PlayerInput playerInput;
    [SerializeField] PlayerLocomotion locomotion;


    private void Awake()
    {
        playerInput.enabled = false;
        locomotion.enabled = false;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer || IsOwner)
        {
            locomotion.enabled = true;
        }
        if (IsOwner)
        {
            playerInput.enabled = true;
        }
    }

    [Rpc(SendTo.Server)]
    private void UpdateInputServerRpc(Vector2 move, Vector2 look, bool jump, bool sprint)
    {
        locomotion.getMovement(move);
        locomotion.getLookRotation(look);
        locomotion.getJumpBool(jump);
        locomotion.getSprintBool(sprint);
    }

    private void LateUpdate()
    {
        // The host owns its player and simulates it directly from local input, so no RPC is needed.
        if (!IsOwner || IsServer) return;

        UpdateInputServerRpc(locomotion.moveInput, locomotion.lookInput, locomotion._jumpTriggered, locomotion._isSprinting);
    }
}
