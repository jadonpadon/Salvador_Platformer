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

    bool dashed;
    public float dashDistance;
    public float dashTime;

    bool wallJump;
    public float wallJumpX;
    float wallJumpTime;

    bool launching; 
    public float launchDistance;
    public float launchTime;
    public Vector2 launchParameters;
    bool canLaunch = false;
    RigidbodyConstraints2D originalConstraints;

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

        rb = GetComponent<Rigidbody2D>();
        collider = GetComponent<Collider2D>();

        originalConstraints = rb.constraints;
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

        if (Input.GetKey(KeyCode.A) && !TouchingLeftWall())
        {
            movingLeft = true;
        }
        if (Input.GetKey(KeyCode.D) && !TouchingRightWall())
        {
            movingRight = true;
        }

        //normal jump
        if (Input.GetKeyDown(KeyCode.Space) && IsGrounded() && !canCoyoteJump)
        {
            jumping = true;
            canCoyoteJump = false;

        }

        //wall jump
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (wallJumpTime > Time.time && Input.GetKeyDown(KeyCode.Space) && (TouchingLeftWall() || TouchingRightWall()))
            {
                wallJump = true;
            }
            else
            {
                wallJumpTime = Time.time + 1f;
            }
        }

        if (Input.GetKeyDown(KeyCode.Space) && !coyoteJumping && canCoyoteJump)
        {
            coyoteJumping = true;
        }

        if (Input.GetMouseButtonDown(0))
        {
            dashed = true;
        }

        if (Input.GetMouseButtonDown(1) && canLaunch)
        {
            canLaunch = false;
            launching = true;
            if (launchTime > Time.time && Input.GetMouseButtonDown(1) && !IsGrounded())
            {
                rb.constraints = RigidbodyConstraints2D.FreezePositionX;

            }
            else
            {
                launchTime = Time.time + 1f;
            }

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


        if (movingLeft && !dashed)
        {
            speed += acceleration * Time.deltaTime;
            playerInput = Vector2.left;
            lastDirectionV2 = Vector2.left;
            rb.velocity = new Vector2(playerInput.x * speed, rb.velocity.y);
            lastDirection = FacingDirection.left;
            isMoving = true;

            movingLeft = false;
        }
        if (movingRight && !dashed)
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
            //unfreeze when landed from launch
            rb.constraints = originalConstraints;
            canLaunch = true;
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
            StartCoroutine(Dash(lastDirectionV2.x));
        }

        IEnumerator Dash(float direction)
        {
            rb.velocity = new Vector2(rb.velocity.x, 0f);
            rb.AddForce(new Vector2(dashDistance * direction, 0f), ForceMode2D.Impulse);
            yield return new WaitForSeconds(dashTime);
            rb.velocity = new Vector2(playerInput.x * speed, rb.velocity.y);
            dashed = false;
        }

        if (wallJump)
        {
            rb.velocity = new Vector2(-lastDirectionV2.x * wallJumpX, initialJumpVelocity);

            wallJump = false;
        }

        if (launching)
        {
            rb.AddForce(new Vector2(lastDirectionV2.x * launchParameters.x * launchDistance, launchParameters.y), ForceMode2D.Impulse);
            launching = false;
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

    public bool TouchingLeftWall()
    {
        RaycastHit2D hit = Physics2D.Raycast(collider.bounds.center, Vector2.left, 0.6f, groundLayer);

        if (hit)
        {
            return true;
        }
        else
        {
            return false;
        }

    }

    public bool TouchingRightWall()
    {
        RaycastHit2D hit = Physics2D.Raycast(collider.bounds.center, Vector2.right, 0.6f, groundLayer);

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
