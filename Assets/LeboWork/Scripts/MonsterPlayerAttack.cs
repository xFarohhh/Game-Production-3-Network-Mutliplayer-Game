using UnityEngine;
using UnityEngine.InputSystem;

public class MonsterPlayerAttack : MonoBehaviour
{
    private Rigidbody rb;
    public bool isGrounded;
    public bool isAttacking;
    public bool isJumping;
    private Vector2 attack;
    public bool isAttack1LSpike;
    public bool isAttack1RSpike;
    private Animator anim;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void AttackInput(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            Attack1();
        }
        attack = context.ReadValue<Vector2>();
    }

    public void Attack1LSpikeInput(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            Attack1LSpike();
        }
        attack = context.ReadValue<Vector2>();
    }

    public void Attack1()
    {
        anim.SetBool("isAttacking", true);
    }

    public void Attack1LSpike()
    {
        anim.SetBool("isAttack1LSpike", true);
    }

    public void Attack1RSpikeInput(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            Attack1RSpike();
        }
        attack = context.ReadValue<Vector2>();
    }

    public void Attack1RSpike()
    {
        anim.SetBool("isAttack1RSpike", true);
    }

    public void EndAttack1()
    {
        anim.SetBool("isAttacking", false);
    }

    public void EndAttack1LSpike()
    {
        anim.SetBool("isAttack1LSpike", false);
    }

    public void EnedAttack1RSpike()
    {
        anim.SetBool("isAttack1RSpike", false);
    }
}
