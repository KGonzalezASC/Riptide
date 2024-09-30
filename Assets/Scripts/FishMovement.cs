using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

//TODO: Refine movement for moving around the "water" and add diving / jumping
//make player affected by custom force (like wind or current) 
//make player that when depth in water is too deep , player will have a different direction of gravity 
//implement a "swim fast" charge meter of somekind
//make player "die"

public class FishMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField]
    private InputAction playerControls; // Input for Movement

    [SerializeField]
    private InputAction jumpAction; // Input for jumping

    [SerializeField]
    private float movementSpeed = 100f;

    [SerializeField]
    private float jumpForce = 8f; // The force applied when jumping

    [SerializeField]
    private float minHeight = 1f; // Define the minimum height

    private Vector2 moveDirection = Vector2.zero;
    private bool isGrounded = true;

    [SerializeField]
    private float friction = 0.05f; // Friction factor for slowing down

    [SerializeField]
    private Rigidbody rb;

    [SerializeField] private Vector3 anchorPoint = Vector3.zero;  // Anchor point to rotate around

    private void OnEnable()
    {
        playerControls.Enable();
        jumpAction.Enable();
    }

    private void OnDisable()
    {
        playerControls.Disable();
        jumpAction.Disable();
    }

    private void Update()
    {
        moveDirection = playerControls.ReadValue<Vector2>();

        // Check if the player is grounded
        isGrounded = transform.position.y <= minHeight;

        // Check for jump input and whether the player is grounded
        if (jumpAction.triggered && isGrounded)
        {
            Jump();
        }
    }


    //TODO: Check if this is depricated
    //private void OnMovePerformed(InputAction.CallbackContext context)
    //{
    //    movementInput = context.ReadValue<Vector2>();
    //}

    //private void OnMoveCanceled(InputAction.CallbackContext context)
    //{
    //    //movementInput = Vector2.zero;
    //}

    private void FixedUpdate()
    {
        // Get the current velocity
        Vector3 currentVelocity = rb.velocity;

        // Apply horizontal movement based on input
        if (moveDirection != Vector2.zero)
        {
            // Calculate new velocity based on input
            currentVelocity.x = moveDirection.x * movementSpeed * Time.deltaTime;
            currentVelocity.z = moveDirection.y * movementSpeed * Time.deltaTime;
        }
        else if (isGrounded) // Apply gradual slow-down when grounded and no input
        {
            // Gradually reduce horizontal velocity using friction
            currentVelocity.x = Mathf.Lerp(currentVelocity.x, 0, friction);
            currentVelocity.z = Mathf.Lerp(currentVelocity.z, 0, friction);
        }

        // Apply the updated velocity to the Rigidbody
        rb.velocity = currentVelocity;

        // Check if the player is below the minimum height
        if (rb.position.y < minHeight)
        {
            Vector3 newPosition = rb.position;
            newPosition.y = minHeight; // Reset the y-position to the minimum height
            rb.position = newPosition;

            // Reset the downward velocity to prevent falling past the minHeight
            rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
        }
    }

    // This function will help visualize the anchor point and movement vector in the editor
    private void OnDrawGizmos()
    {
        // Draw the anchor point in the scene view
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(anchorPoint, 0.2f);  // Visualize the anchor point
    }

    // Function to make the player jump
    private void Jump()
    {
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse); // Apply upward force to jump
    }
}
