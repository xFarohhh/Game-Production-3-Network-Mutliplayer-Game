using UnityEngine;
using UnityEngine.InputSystem;
//Note for this script to function as intended the Gameobject that this is attached to must have the following: A Character Controller, PlayerInput setup for Unity Events. 
// And then assign the events to the corresponding actions

// Also note this movement script is made to accomodate sprinting if you do not want the player to sprint remove the sprintVariables and the sprint region. 
// After deleting those you want to navigate to the movement region and change all "currentSpeed" Variables to walkSpeed
public class PlayerLocomotion : MonoBehaviour
{

    [Header("Player Input Asset")]
    private PlayerInput _playerInput;

    [Header("Player Movement Settings")]
    [SerializeField] float _walkSpeed;
    private CharacterController _characterController;

    [Tooltip("moveInput logs the input from the chosen device and feeds it to currentMovement to drive the player in the direction of the input")]
    private Vector2 _moveInput;
    private Vector3 _currentMovement;

    [Header("Stamina Settings")]
    [Range(0,100)]public float Stamina;

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
     private bool _isSprinting;

    [Header("Jump Settings")]
    [SerializeField] float _jumpForce = 5f;
    [SerializeField] float gravityMultiplier = 1f;
    [Tooltip("This variable is used for input registration")]
    private bool _jumpTriggered;

    [Header("Rotation Settings")]
    [SerializeField] float mouseSensitivity = 0.1f;
    [SerializeField] float gamepadSensitivity = 2f;
    [SerializeField] Camera mainCamera;
    [Tooltip("Vertical Range determines how much the camera can rotate vertically")]
    float verticalRange = 88f;
    float verticalRotation;
    private bool updatingRotation = true;
    [Tooltip("This variable is used for input registration")]
    private Vector2 lookInput;

    private void Awake()
    {
        _playerInput = GetComponent<PlayerInput>();
        _characterController = GetComponent<CharacterController>();
    }

    private void Update()
    {
        handleMovement();
        handleRotation();
        regenerateStamina();
    }
    #region Movement
    public void OnMove(InputAction.CallbackContext context){
        _moveInput = context.ReadValue<Vector2>();
    }
    // purpose of this function is to calculate which direction the player is moving in and ensuring that the player moves in the direction that they are looking in.
    private Vector3 calculateWorldDirecion()
    {
        Vector3 inputDirection = new Vector3(_moveInput.x,0,_moveInput.y);
        Vector2 worldDirection = transform.TransformDirection(inputDirection);
        return worldDirection.normalized;
    }
    private void handleMovement(){
        Vector3 worldDirection = calculateWorldDirecion();
        _currentMovement.x = worldDirection.x * _currentSpeed;
        _currentMovement.z = worldDirection.z * _currentSpeed;

        handleJumping();
        _characterController.Move(_currentMovement * Time.deltaTime);
    }

    public void OnJump(InputAction.CallbackContext context){
        _jumpTriggered = context.performed;
    }
    private void handleJumping(){
        if(_characterController.isGrounded){
            _currentMovement.y = -.5f;

            if(_jumpTriggered && Stamina >=1){
                takeStamina(20);
                _currentMovement.y = _jumpForce;
            }
        }

        // applies gravity to the player
        else
        {
            _currentMovement.y += Physics.gravity.y * gravityMultiplier * Time.deltaTime;
        }
    }
    public void OnSprint(InputAction.CallbackContext context){
        _isSprinting = context.ReadValueAsButton();
    }
    #endregion Movement

    #region Stamina
    private void takeStamina(float amount){
        Stamina -= amount;
        // ensures that Stamina can never be negative.
        if(Stamina <=0){
            Stamina = 0;
        }
    }
    private void drainStamina(float amount){
        Stamina -= (amount * Time.deltaTime);
        if(Stamina <=0){
            Stamina = 0;
        }
    }
    private void regenerateStamina()
    {
        float regenerateDefaultTime = _regenerateDelay;
        // only allows for the player to regnerate stamina if the timer has ran out.
        if(Stamina < 0){
            _regenerateDelay -= 1 * Time.deltaTime;
        }
        if(_regenerateDelay <=0){
            Stamina += _staminaGain * Time.deltaTime;
        }
        if(Stamina >= 100){
            Stamina = _maxStamina;
            _regenerateDelay = regenerateDefaultTime;
        }
        if(_isSprinting || _jumpTriggered){
            _regenerateDelay = regenerateDefaultTime;
        }
    }
    #endregion Stamina

    #region Rotation
    public void OnLook(InputAction.CallbackContext context){
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

        switch(_playerInput.currentControlScheme){
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

    

    
}