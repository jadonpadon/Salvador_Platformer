using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private Rigidbody2D rb;
    private new Collider2D collider;
    private bool movingLeft = false;
    private bool movingRight = false;
    public float maxSpeed;
    public float accelerationTime;
    public float decelerationTime;
    float speed;
    Vector2 lastDirectionV2 = Vector2.zero;

    private bool jumping = false;
    public float apexHeight;
    public float apexTime;
    private float gravity;
    private float initialJumpVelocity;
    public float terminalSpeed;
    bool grounded = false;
    
    public float coyoteTime;
    private IEnumerator coyoteTimeCoroutine;
    bool canCoyoteJump = false;
    bool coyoteJumping = false;
    bool coyoteTimerStarted = false;

    public enum FacingDirection
    {
        left, right
    }

    private FacingDirection lastDirection;
    public LayerMask groundLayer;

    // Start is called before the first frame update
    void Start()
    {
        initialJumpVelocity = 2 * apexHeight / apexTime;
        gravity = -2 * apexHeight / (Mathf.Pow(apexTime, 2));

        rb = GetComponent<Rigidbody2D>();
        collider = GetComponent<Collider2D>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        //The input from the player needs to be determined and then passed in the to the MovementUpdate which should
        //manage the actual movement of the character.
        Vector2 playerInput = new Vector2();
        MovementUpdate(playerInput);
    }

    private void Update()
    {
        if (Input.GetKey(KeyCode.A))
        {
            movingLeft = true;
        }
        if (Input.GetKey(KeyCode.D))
        {
            movingRight = true;
        }
        if (Input.GetKeyDown(KeyCode.Space) && grounded && !canCoyoteJump)
        {
            jumping = true;
            canCoyoteJump = false;
        }

        if (Input.GetKeyDown(KeyCode.Space) && !coyoteJumping && canCoyoteJump)
        {
            coyoteJumping = true;
        }

        if (canCoyoteJump)
        {
            print("can coyote jump");
        }

        if (jumping)
        {
            print("jumping");
        }

        if (coyoteJumping)
        {
            print ("coyote jumping");
        }
    }

    private void MovementUpdate(Vector2 playerInput)
    {
        float acceleration = maxSpeed / accelerationTime;
        float deceleration = maxSpeed / decelerationTime;

        if (speed > maxSpeed)
        {
            speed = maxSpeed;
        }

        bool isMoving = false;


        if (movingLeft)
        {
            speed += acceleration * Time.deltaTime;
            playerInput = Vector2.left;
            lastDirectionV2 = Vector2.left;
            rb.velocity = new Vector2(playerInput.x * speed, rb.velocity.y);
            lastDirection = FacingDirection.left;
            isMoving = true;

            movingLeft = false;
        }
        if (movingRight)
        {
            speed += acceleration * Time.deltaTime;
            playerInput = Vector2.right;
            lastDirectionV2 = Vector2.right;
            rb.velocity = new Vector2(playerInput.x * speed, rb.velocity.y);
            lastDirection = FacingDirection.right;
            isMoving = true;

            movingRight = false;
        }
        if (jumping && grounded)
        {
            rb.velocity = new Vector2(rb.velocity.x, initialJumpVelocity);
        }

        if (coyoteJumping && canCoyoteJump)
        {
            rb.velocity = new Vector2(rb.velocity.x, initialJumpVelocity);

            canCoyoteJump = false;
        }

        if (!grounded)
        {
            //stop coyote timer

            coyoteTimerStarted = false;

            //gravity

            rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y + gravity * Time.fixedDeltaTime);
            if (rb.velocity.y < terminalSpeed)
            {
                rb.velocity = new Vector2(rb.velocity.x, terminalSpeed);
            }
        }
        else if (grounded)
        {
            coyoteJumping = false;
            jumping = false;
        }

        if (!grounded && !jumping && !coyoteTimerStarted && !coyoteJumping)
        {
            coyoteTimeCoroutine = CoyoteTime(coyoteTime);

            StartCoroutine(coyoteTimeCoroutine);

        }


        if (!isMoving && speed > 0)
        {
            speed -= deceleration * Time.fixedDeltaTime;
            if (speed < 0) speed = 0;

            rb.velocity = new Vector2(lastDirectionV2.x * speed, rb.velocity.y);

        }

    }

    private IEnumerator CoyoteTime(float waitTime)
    {
        coyoteTimerStarted = true;
        canCoyoteJump = true;

        yield return new WaitForSeconds(waitTime);
        coyoteTimerStarted = false;
        canCoyoteJump = false;
    }

    public bool IsWalking()
    {
        if(Mathf.Abs(rb.velocity.x) > 0)
        {
            return true;
        }
        else
        {
            return false;
        }
    }
    public bool IsGrounded()
    {
        RaycastHit2D hit = Physics2D.BoxCast(collider.bounds.center, collider.bounds.size, 0f, Vector2.down, 0.1f, groundLayer);
        
        if (hit)
        {
            grounded = true;
            return true;
        }
        else
        {
            grounded = false;
            return false;
        }

    }

    public FacingDirection GetFacingDirection()
    {
        if(rb.velocity.x > 0)
        {
            return FacingDirection.right;
        }
        else if(rb.velocity.x < 0) 
        { 
            return FacingDirection.left; 
        }
        return lastDirection;
    }
}
