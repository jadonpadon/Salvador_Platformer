using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using Unity.VisualScripting;
using UnityEditor.XR;
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
    
    public float coyoteTime;
    private IEnumerator coyoteTimeCoroutine;
    bool canCoyoteJump = false;
    bool coyoteJumping = false;
    bool coyoteTimerStarted = false;

    public int currentHealth;

    bool dashed = false;
    Vector2 currentPos;
    Vector2 dashDestination;
    public float dashDistance;
    public float dashSpeed;
    float distance2destination;
    float storeMaxSpeed;

    public enum FacingDirection
    {
        left, right
    }

    public enum CharacterState
    {
        idle, walk, jump, die
    }
    public CharacterState currentState = CharacterState.idle;
    public CharacterState previousState = CharacterState.idle;


    private FacingDirection lastDirection;
    public LayerMask groundLayer;

    // Start is called before the first frame update
    void Start()
    {
        initialJumpVelocity = 2 * apexHeight / apexTime;
        gravity = -2 * apexHeight / (Mathf.Pow(apexTime, 2));

        distance2destination = Vector2.Distance(currentPos, dashDestination);

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
        previousState = currentState;

        if (IsDead())
        {
            currentState = CharacterState.die;
        }

        switch (currentState)
        {
            case CharacterState.idle:
                if (IsWalking())
                {
                    currentState = CharacterState.walk;
                }
                if (!IsGrounded())
                {
                    currentState = CharacterState.jump;
                }
                break;
            case CharacterState.walk:
                if (!IsWalking())
                {
                    currentState = CharacterState.idle;
                }
                if (!IsGrounded())
                {
                    currentState = CharacterState.jump;
                }
                break;
            case CharacterState.jump:
                if (IsGrounded())
                {
                    if (IsWalking())
                    {
                        currentState = CharacterState.walk;
                    }
                    else
                    {
                        currentState = CharacterState.idle;
                    }
                }
                break;
            case CharacterState.die:

                break;
        }

        if (Input.GetKey(KeyCode.A))
        {
            movingLeft = true;
        }
        if (Input.GetKey(KeyCode.D))
        {
            movingRight = true;
        }
        if (Input.GetKeyDown(KeyCode.Space) && IsGrounded() && !canCoyoteJump)
        {
            jumping = true;
            canCoyoteJump = false;
        }

        if (Input.GetKeyDown(KeyCode.Space) && !coyoteJumping && canCoyoteJump)
        {
            coyoteJumping = true;
        }

        if (Input.GetMouseButtonDown(0))
        {
            dashed = true;
            currentPos = rb.position;
            dashDestination = currentPos + lastDirectionV2.normalized * dashDistance; 
            storeMaxSpeed = maxSpeed;

            print("dashed");
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
        if (jumping && IsGrounded())
        {
            rb.velocity = new Vector2(rb.velocity.x, initialJumpVelocity);
        }

        if (coyoteJumping && canCoyoteJump)
        {
            rb.velocity = new Vector2(rb.velocity.x, initialJumpVelocity);

            canCoyoteJump = false;
        }

        if (!IsGrounded())
        {
            //gravity

            rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y + gravity * Time.fixedDeltaTime);
            if (rb.velocity.y < terminalSpeed)
            {
                rb.velocity = new Vector2(rb.velocity.x, terminalSpeed);
            }
        }
        else if (IsGrounded())
        {
            coyoteJumping = false;
            jumping = false;
            coyoteTimerStarted = false;
        }

        if (!IsGrounded() && !jumping && !coyoteTimerStarted && !coyoteJumping)
        {
            coyoteTimeCoroutine = CoyoteTime(coyoteTime);

            StartCoroutine(coyoteTimeCoroutine);

        }


        if (!isMoving && speed > 0)
        {
            //decelerate
            
            speed -= deceleration * Time.fixedDeltaTime;
            if (speed < 0) speed = 0;

            rb.velocity = new Vector2(lastDirectionV2.x * speed, rb.velocity.y);

        }

        if (dashed)
        {
            // Calculate distance to destination
            distance2destination = Vector2.Distance(rb.position, dashDestination);

            if (distance2destination > 0.1f)
            {
                // Execute dash
                maxSpeed *= dashSpeed;
                rb.velocity = lastDirectionV2.normalized * maxSpeed;
            }
            else
            {
                // Reset speed and stop dashing
                maxSpeed = storeMaxSpeed;
                dashed = false;
            }

        }

    }

    private void Dash()
    {
        storeMaxSpeed = maxSpeed;
        
        maxSpeed *= dashSpeed;
        rb.velocity = new Vector2(lastDirectionV2.x * maxSpeed, rb.velocity.y);
        
        if (distance2destination < 0.1f)
        {
            maxSpeed = storeMaxSpeed;
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
        RaycastHit2D hit = Physics2D.Raycast(collider.bounds.center, Vector2.down, 0.6f, groundLayer);
        
        if (hit)
        {
            return true;
        }
        else
        {
            return false;
        }

    }

    public bool IsDead()
    {
        return currentHealth <= 0;
    }

    public void OnDeathAnimationComplete()
    {
        gameObject.SetActive(false);
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
