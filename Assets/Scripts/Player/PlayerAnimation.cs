using UnityEngine;
using UnityEngine.InputSystem;
public class PlayerAnimation : MonoBehaviour
{
    public Rigidbody rb;
    public Animator anim;
    private Vector2 movement;
    private Vector2 jump;
    public float speed = 10.2f;
    public float jumpSpeed = 2f;
    private float jumpForce = 3f;
    public bool isRunning;
    private bool isGrounded = true;
    private int moveSpeed = 6; 

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        //determine if we are moving
        if (movement.magnitude > 0.1)
        {
            anim.SetBool("isRunning", true);
        }

        else
        {
            anim.SetBool("isRunning", false);
        }


        anim.SetBool("isJumping", !isGrounded);
        //anim.SetBool("isRunning", Mathf.Abs(movement.x) > 0.1f);
    }

    private void FixedUpdate()
    {
        Move();
        Debug.Log(movement);
        Debug.Log(jump);
    }

    public void MovementInput(InputAction.CallbackContext context)
    {
        movement = context.ReadValue<Vector2>();
        Debug.Log(movement);
    }

    public void Move()
    {
        rb.linearVelocity = new Vector3(movement.x * moveSpeed, rb.linearVelocity.y, movement.y * moveSpeed);
    }

    public void JumpInput(InputAction.CallbackContext context)
    {
        if (context.performed && isGrounded)
        {
            Jump();

        }

        rb.AddForce(jumpSpeed * transform.up, ForceMode.Impulse);
        isGrounded = false;

        Debug.Log(jump);
    }

    public void Jump()
    {
        
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;
        }
        
    }



}
