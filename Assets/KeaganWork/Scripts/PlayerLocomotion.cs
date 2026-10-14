using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;      // ADDED: list of every spawned player (used by the flashlight kill scan)
using Unity.Netcode.Components;

public class PlayerLocomotion : NetworkBehaviour
{
    [Header("Player Input Asset")]
    private PlayerInput _playerInput;

    [Header("Player Movement Settings")]
    [SerializeField] float _walkSpeed;
    private CharacterController _characterController;

    [Tooltip("moveInput logs the input from the chosen device and feeds it to currentMovement to drive the player in the direction of the input")]
    public Vector2 moveInput;
    private Vector3 _currentMovement;

    [Header("Stamina Settings")]
    [Range(0, 100)] public float Stamina;

    [Tooltip("Stamina gain is the amount of stamina that the player gains after using an action that drains stamina")]
    [SerializeField] float _staminaGain;

    [Tooltip("RegenerateDelay is used to delay the timing of when the player can start regenerating stamina")]
    [SerializeField] float _regenerateDelay;

    private float _maxStamina = 100;

    [Header("Sprint Settings")]
    [SerializeField] float sprintMultiplier;

    private bool _sprintActive => isSprinting && Stamina > 0;
    private float _currentSpeed => _walkSpeed * (_sprintActive ? sprintMultiplier : 1);

    [Tooltip("This variable is used for input registration")]
    public bool isSprinting;

    [Header("Jump Settings")]
    [SerializeField] float _jumpForce = 5f;
    [SerializeField] float gravityMultiplier = 1f;
    [Tooltip("This variable is used for input registration")]
    public bool _jumpTriggered;

    [Header("Rotation Settings")]
    [SerializeField] float mouseSensitivity = 0.1f;
    [SerializeField] float gamepadSensitivity = 2f;
    [SerializeField] Camera mainCamera;
    [Tooltip("Vertical Range determines how much the camera can rotate vertically")]
    float verticalRange = 88f;
    float verticalRotation;
    private bool updatingRotation = true;
    [Tooltip("This variable is used for input registration")]
    public Vector2 lookInput;

    [Header("Flashlight Settings")]
    [SerializeField] float currentBatteryLife;
    [SerializeField] float maxBatteryLife = 100;
    [SerializeField] float batteryRegenSpeed = 2.5f;
    [SerializeField] float batteryDrainSpeed = 4f;
    [SerializeField] Slider batterSlider;
    [SerializeField] TextMeshProUGUI batteryLifeText;
    [SerializeField] Light light;
    [SerializeField] bool isFlashLightActive;

    [Header("Animator")]
    private Animator anim;

    [Header("Death Variables")]
    [SerializeField] Button respawnButton;
    [SerializeField] bool isPlayerDead;
    [SerializeField] GameObject PlayerHumanMesh;
    [SerializeField] GameObject playerDustMesh;

    private NetworkVariable<bool> netIsDead = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private NetworkVariable<bool> netFlashlightOn = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private NetworkVariable<float> netPitch = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private static readonly List<PlayerLocomotion> AllPlayers = new List<PlayerLocomotion>();

    private void Awake()
    {
        _playerInput = GetComponent<PlayerInput>();
        _characterController = GetComponent<CharacterController>();
   
    }


    private void Update()
    {
        if (!IsOwner) return;

        PlayerStateManager state = GetComponent<PlayerStateManager>();
        if (state != null && !state.CanMove()) return;

        handleMovement();
        regenerateStamina();
        handleRotation();
    }

    public override void OnNetworkSpawn()
    {
        
    }

    #region Movement
    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    private Vector3 calculateWorldDirecion()
    {
        Vector3 inputDirection = new Vector3(moveInput.x,0,moveInput.y);
        Vector3 worldDirection = transform.TransformDirection(inputDirection);
        return worldDirection.normalized;
    }

    private void handleMovement()
    {
        Vector3 worldDirection = calculateWorldDirecion();
        _currentMovement.x = worldDirection.x * _currentSpeed;
        _currentMovement.z = worldDirection.z * _currentSpeed;

        handleJumping();
        _characterController.Move(_currentMovement * Time.deltaTime);
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        _jumpTriggered = context.performed;
    }

    private void handleJumping()
    {
        if (_characterController.isGrounded)
        {
            _currentMovement.y = -.5f;

            if (_jumpTriggered && Stamina >= 1)
            {
                takeStamina(20);
                _currentMovement.y = _jumpForce;
            }
        }
        else
        {
            _currentMovement.y += Physics.gravity.y * gravityMultiplier * Time.deltaTime;
        }
    }

    public void OnSprint(InputAction.CallbackContext context)
    {
        isSprinting = context.ReadValueAsButton();
    }
    #endregion Movement

    #region Stamina
    private void takeStamina(float amount)
    {
        Stamina -= amount;
        if (Stamina <= 0)
        {
            Stamina = 0;
        }
    }

    private void drainStamina(float amount)
    {
        Stamina -= (amount * Time.deltaTime);
        if (Stamina <= 0)
        {
            Stamina = 0;
        }
    }

    private void regenerateStamina()
    {
        float regenerateDefaultTime = _regenerateDelay;
        if (Stamina < 0)
        {
            _regenerateDelay -= 1 * Time.deltaTime;
        }
        if (_regenerateDelay <= 0)
        {
            Stamina += _staminaGain * Time.deltaTime;
        }
        if (Stamina >= 100)
        {
            Stamina = _maxStamina;
            _regenerateDelay = regenerateDefaultTime;
        }
        if (isSprinting || _jumpTriggered)
        {
            _regenerateDelay = regenerateDefaultTime;
        }
    }
    #endregion Stamina

    #region Rotation
    public void OnLook(InputAction.CallbackContext context)
    {
        lookInput = context.ReadValue<Vector2>();
    }

    private void applyHorizontalRotation(float rotationAmount)
    {
        transform.Rotate(0, rotationAmount, 0);
    }

    private void applyVerticalRotation(float rotationAmount)
    {
        verticalRotation = Mathf.Clamp(verticalRotation - rotationAmount, -verticalRange, verticalRange);
        mainCamera.transform.localRotation = Quaternion.Euler(verticalRotation, 0, 0);
    }

    private void handleRotation(){
        if(!updatingRotation) return;
        switch (_playerInput.currentControlScheme)
        {
            case "Mouse & Keyboard":
                float mouseXRotation = lookInput.x * mouseSensitivity;
                float mouseYRotation = lookInput.y * mouseSensitivity;
                applyHorizontalRotation(mouseXRotation);
                applyVerticalRotation(mouseYRotation);
                break;

            case "Gamepad":
                float gamepadXRotation = lookInput.x * gamepadSensitivity;
                float gamepadYRotation = lookInput.y * gamepadSensitivity;
                applyHorizontalRotation(gamepadXRotation);
                applyVerticalRotation(gamepadYRotation);
                break;
        }
    }
    #endregion Rotation

    #region Flashlight
    public void OnActivateFlashLight(InputAction.CallbackContext context)
    {
        isFlashLightActive = context.performed;
    }

    private void handleFlashLightBattery()
    {
      bool lightOn = isFlashLightActive && !isPlayerDead && currentBatteryLife > 0 && !isSprinting && !_jumpTriggered;

        if (lightOn)
        {
            currentBatteryLife = Mathf.Max(0, currentBatteryLife - batteryDrainSpeed * Time.deltaTime); // drains batter life over time
        }
        else
        {
            currentBatteryLife = Mathf.Min(maxBatteryLife, currentBatteryLife + batteryRegenSpeed * Time.deltaTime);
        }
        handleFlashLight(lightOn);


    }

    private void handleFlashLight(bool isOn)
    {
        if(light != null)
        {
            light.enabled = isOn && !isPlayerDead;
        }
    }
    #endregion Flashlight

    #region Networking Reference Getters
    public Vector2 getMovement(Vector2 move)
    {
        moveInput = move;   
        return move;
    }
    public Vector2 getLookRotation(Vector2 look)
    {
        lookInput = look;
        return look;
    }
    public bool getJumpBool(bool isJumping)
    {
        _jumpTriggered = isJumping;
        return isJumping;

    }
    public bool getSprintBool(bool IsSprinting)
    {
        isSprinting = IsSprinting;
        return isSprinting;
    }

    public float getMouseSens()
    {
        return mouseSensitivity;
    }
    public float getGamepadSens()
    {
        return gamepadSensitivity;
    }
    #endregion Networking Reference Getters

    public void respawn()
    {
        
    }

    public void spawnPlayer()
    {

    }
}
