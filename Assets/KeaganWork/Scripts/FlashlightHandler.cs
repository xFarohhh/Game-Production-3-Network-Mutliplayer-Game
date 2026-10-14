using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class FlashlightHandler : NetworkBehaviour
{
    [Header("Flashlight Settings")]
    [SerializeField] float currentBatteryLife;
    [SerializeField] float maxBatteryLife = 100;
    [SerializeField] float batteryRegenSpeed = 2.5f;
    [SerializeField] float batteryDrainSpeed = 4f;
    [SerializeField] Light light;
    [SerializeField] bool isFlashLightActive;
    [SerializeField] Camera mainCamera;

    [Header("Flashlight Kill Settings")]
    [Tooltip("Assign the player so the Flashlight can detect and kill the player")]
    [SerializeField] LayerMask playerLayer;
    [SerializeField] LayerMask obstacleLayers;
    [SerializeField] float timeToDie = 1f;

    // Set by the movement script, for example false while sprinting or jumping (the flashlight turns off)
    [HideInInspector] public bool canUseFlashlight = true;

    // synced state, written by the owner
    private NetworkVariable<bool> netFlashlightOn = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private NetworkVariable<float> netPitch = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private static readonly List<FlashlightHandler> AllFlashlights = new List<FlashlightHandler>();
    private bool isDisabled;
    private float timeInFlashlight;

    public event Action isPlayerDead;

    private void Awake()
    {
        if (mainCamera == null) mainCamera = GetComponentInChildren<Camera>(true);
        ResolveLight();
    }

    private void Update()
    {
        if (!IsOwner || !IsSpawned || isDisabled) return;

        handleFlashLightBattery();
        checkFlashlightHit();
    }

    public override void OnNetworkSpawn()
    {
        AllFlashlights.Add(this);
        handleFlashLight(netFlashlightOn.Value);

        if (IsOwner)
        {
            currentBatteryLife = maxBatteryLife; // the battery was serialized as 0, which meant the flashlight could never turn on
        }

        base.OnNetworkSpawn();
    }

    public override void OnNetworkDespawn()
    {
        AllFlashlights.Remove(this);
        base.OnNetworkDespawn();
    }

    #region Input
    public void OnActivateFlashLight(InputAction.CallbackContext context)
    {
        isFlashLightActive = context.performed;
    }
    #endregion Input

    #region Battery
    // The battery drains while the light is on and regenerates while it is off. The on / off state is synced through netFlashlightOn
    // so every player can see (and be hit by) the beam.
    private void handleFlashLightBattery()
    {
        bool lightOn = isFlashLightActive && canUseFlashlight && currentBatteryLife > 0;

        if (lightOn)
        {
            currentBatteryLife = Mathf.Max(0, currentBatteryLife - batteryDrainSpeed * Time.deltaTime);
        }
        else
        {
            currentBatteryLife = Mathf.Min(maxBatteryLife, currentBatteryLife + batteryRegenSpeed * Time.deltaTime);
        }

        handleFlashLight(lightOn);
        if (netFlashlightOn.Value != lightOn) netFlashlightOn.Value = lightOn;
    }

    // EDITED: the battery UI was removed from this script so it has no UI references. Read the battery here and show it from the movement / UI script.
    public float BatteryLife => currentBatteryLife;
    public float MaxBatteryLife => maxBatteryLife;
    public float BatteryNormalized => currentBatteryLife / maxBatteryLife; // 0 - 1
    #endregion Battery

    #region Light
    // Finds (or creates) the Light used by the flashlight.
    private void ResolveLight()
    {
        if (light == null) light = GetComponentInChildren<Light>(true);
        if (light == null)
        {
            GameObject lightObject = new GameObject("FlashlightLight");
            lightObject.transform.SetParent(mainCamera != null ? mainCamera.transform : transform, false);
            light = lightObject.AddComponent<Light>();
            light.type = LightType.Spot;
            light.range = 15f;
            light.spotAngle = 40f;
            light.intensity = 3f;
        }
        light.enabled = false;
    }

    private void handleFlashLight (bool isLightOn)
    {
        if (light != null) light.enabled = isLightOn && !isDisabled;
    }
    #endregion Light

    #region Kill
    private bool isPlayer(GameObject target)
    {
        return (playerLayer.value & (1 << target.layer)) != 0;
    }

    // Runs on the owner of this player: checks every other player's flashlight (range, cone angle, line of sight).
    // Staying in a beam for timeToDie seconds raises OnKilledByFlashlight, stepping out of the beam resets the timer.
    private void checkFlashlightHit()
    {
        if (!isPlayer(gameObject)) return;

        // aim at the middle of the body, not the feet
        Vector3 bodyPoint = transform.position + Vector3.up;

        bool isLit = false;

        foreach (FlashlightHandler other in AllFlashlights)
        {
            if (other == null || other == this) continue;
            if (!other.netFlashlightOn.Value || other.light == null) continue;

            Vector3 origin = other.light.transform.position;
            Vector3 toBody = bodyPoint - origin;
            float halfAngle = other.light.type == LightType.Spot ? other.light.spotAngle * 0.5f : 30f;

            if (toBody.magnitude > other.light.range) continue;
            if (Vector3.Angle(other.light.transform.forward, toBody) > halfAngle) continue;

            // walls and other geometry block the beam
            if (Physics.Raycast(origin, toBody, out RaycastHit hit, toBody.magnitude, obstacleLayers, QueryTriggerInteraction.Ignore)
                && !hit.transform.IsChildOf(transform) && !hit.transform.IsChildOf(other.transform))
            {
                continue;
            }

            isLit = true;
            break;
        }

        if (isLit)
        {
            timeInFlashlight += Time.deltaTime;
            if (timeInFlashlight >= timeToDie)
            {
                timeInFlashlight = 0f;
                if (timeInFlashlight >= timeToDie) OnPlayerKilled();
            }
        }
        else
        {
            timeInFlashlight = 0f;
        }
    }
    private void OnPlayerKilled()
    {
        isFlashLightActive = false;
        if (netFlashlightOn.Value)
        {
            netFlashlightOn.Value = false;
        }
        handleFlashLight(false);
        isPlayerDead?.Invoke();


    }
    #endregion Kill

    #region Control
    // Turns the flashlight off and stops it working (used while the player is dead)
    public void turnLightOffOnDeath(bool isDead)
    {
        isDisabled = isDead;
        timeInFlashlight = 0f;

        if (isDead)
        {
            isFlashLightActive = false;
            if (light != null) light.enabled = false;
        }
    }

    // Refills the battery and resets the shared angle (used after respawning)
    public void ResetFlashlight()
    {
        currentBatteryLife = maxBatteryLife;
    }
    #endregion Control
}
