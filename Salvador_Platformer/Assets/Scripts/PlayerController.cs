using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private Rigidbody2D rb;
    private new Collider2D collider;
    public float speed;

    public enum FacingDirection
    {
        left, right
    }

    private FacingDirection lastDirection;
    public LayerMask groundLayer;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        collider = GetComponent<Collider2D>();
    }

    // Update is called once per frame
    void Update()
    {
        //The input from the player needs to be determined and then passed in the to the MovementUpdate which should
        //manage the actual movement of the character.
        Vector2 playerInput = new Vector2();
        MovementUpdate(playerInput);

        
    }

    private void MovementUpdate(Vector2 playerInput)
    {
        if (Input.GetKey(KeyCode.A))
        {
            playerInput = Vector2.left;
            lastDirection = FacingDirection.left;
        }
        if (Input.GetKey(KeyCode.D))
        {
            playerInput = Vector2.right;
            lastDirection = FacingDirection.right;
        }

        rb.velocity = new Vector2(playerInput.x * speed, rb.velocity.y);
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
            return true;
        }
        else
        {
            Debug.Log("in air");
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
