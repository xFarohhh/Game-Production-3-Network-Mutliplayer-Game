using UnityEngine;
using UnityEngine.InputSystem;
public class Player2Animation : MonoBehaviour
{
    public Rigidbody rb;
    public Animator anim;
    private Vector2 movement;
    private Vector2 jump;
    public float speed = 10.6f;
    public float jumpSpeed = 4.2f;
    private float jumpForce = 2.6f;
    private bool IsJumping;
    public bool IsRunning;
    private bool IsGrounded = true;
    private int moveSpeed = 6;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        anim = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        //check if player2 is moving
        if (movement.magnitude > 0.1)
        {
            anim.SetBool("IsRunning", true);
        }

        else
        {
            anim.SetBool("IsRunning", false);
        }


        anim.SetBool("IsJumping", !IsGrounded);
    }

    private void FixedUpdate()
    {
        Debug.Log(movement);
        Debug.Log(jump);
        Move();
    }

   public void MovementInput(InputAction.CallbackContext context)
    {
        movement = context.ReadValue<Vector2>();
    }

    void Move()
    {
        rb.linearVelocity = new Vector3(movement.x * moveSpeed, rb.linearVelocity.y, movement.y * moveSpeed);
    }

    public void JumpInput(InputAction.CallbackContext context)
    {
        if (context.performed && IsGrounded)
        {
            if (context.performed && IsGrounded)
            {
                
                Jump();
            }
        }
    }

    void Jump()
    {
        rb.AddForce(jumpSpeed * transform.up, ForceMode.Impulse);
        IsGrounded = false;
    }

    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.CompareTag("Ground"))
        {
            IsGrounded = true;
        }
    }

  

}
