using UnityEngine;
using UnityEngine.InputSystem;

public class MonsterLocomotion : MonoBehaviour
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

    private bool _sprintActive => _isSprinting && Stamina > 0;
    private float _currentSpeed => _walkSpeed * (_sprintActive ? sprintMultiplier : 1);

    [Tooltip("This variable is used for input registration")]
    public bool _isSprinting;

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

    private void Awake()
    {
        _playerInput = GetComponent<PlayerInput>();
        _characterController = GetComponent<CharacterController>();
    }

    private void Update()
    {
        handleMovement();
        regenerateStamina();
        handleRotation();
    }
    #region Movement
    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }
    // purpose of this function is to calculate which direction the player is moving in and ensuring that the player moves in the direction that they are looking in.
    private Vector3 calculateWorldDirecion()
    {
        Vector3 inputDirection = new Vector3(moveInput.x, 0, moveInput.y);
        Vector3 worldDirection = transform.TransformDirection(inputDirection);
        return worldDirection.normalized;
    }
    private void handleMovement()
    {
        Vector3 worldDirection = calculateWorldDirecion();
        _currentMovement.x = worldDirection.x * _currentSpeed;
        _currentMovement.z = worldDirection.z * _currentSpeed;

        _characterController.Move(_currentMovement * Time.deltaTime);
    }

    public void OnSprint(InputAction.CallbackContext context)
    {
        _isSprinting = context.ReadValueAsButton();
    }
    #endregion Movement

    #region Stamina
    private void takeStamina(float amount)
    {
        Stamina -= amount;
        // ensures that Stamina can never be negative.
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
        // only allows for the player to regnerate stamina if the timer has ran out.
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

    private void handleRotation()
    {
        if (!updatingRotation) return;
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
    public bool getSprintBool(bool isSprinting)
    {
        _isSprinting = isSprinting;
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
    #endregion
}