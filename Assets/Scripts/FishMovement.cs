using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

enum fishState
{
    SURFACE,
    JUMPING,
    DIVING,
    GRINDING
}

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
    private float buoyancy = 20f;

    [SerializeField]
    private float minHeight = 1f; // Define the minimum height

    private Vector2 moveDirection = Vector2.zero;
    private fishState state = fishState.SURFACE;

    [SerializeField]
    private float friction = 0.05f; // Friction factor for slowing down

    [SerializeField]
    private Rigidbody rb;

    [SerializeField] private Vector3 anchorPoint = Vector3.zero;  // Anchor point to rotate around
    private Vector2 distFromAnchor;


    [Header("Movement Constraints")]
    [SerializeField]
    private float maxAngle = 45f;  // Maximum angle (in degrees) left/right from anchor point

    [SerializeField]
    private float maxRange = 5f;   // Maximum distance from anchor point
    private void OnEnable()
    {
        playerControls.Enable();
        jumpAction.Enable();
        transform.Rotate(0, 180, 0);
    }

    private void OnDisable()
    {
        playerControls.Disable();
        jumpAction.Disable();
    }

    private void Update()
    {
        // Check if the player is grounded
        isGrounded = transform.position.y <= minHeight;
        // Reset jump flag when grounded
        if (isGrounded)
        {
            canJump = true; // Reset jump availability
        }
        if (GameManager.instance.topState.GetName() == "Game")
        {
            moveDirection = playerControls.ReadValue<Vector2>();
            if (jumpAction.triggered && canJump)
            {
                Jump();
                canJump = false; // Prevent further jumps until grounded again
            }
        }
    }

    private void FixedUpdate()
    {
        // Get the current velocity
        Vector3 currentVelocity = rb.velocity;

        // Preserve the current vertical velocity (Y) so jumping and gravity aren't overwritten
        float verticalVelocity = currentVelocity.y;

        // Apply horizontal movement based on input
        if (moveDirection != Vector2.zero)
        {
            // Smoothly interpolate the horizontal movement
            float targetXVelocity = moveDirection.x * movementSpeed * Time.deltaTime;
            currentVelocity.x = Mathf.Lerp(currentVelocity.x, targetXVelocity, friction);
        }
        else if (state == fishState.SURFACE || state == fishState.DIVING) // Apply gradual slow-down when grounded and no input
        {
            // Gradually reduce horizontal velocity using friction
            currentVelocity.x = Mathf.Lerp(currentVelocity.x, 0, friction);
        }

        // Calculate new position after applying horizontal velocity
        Vector3 newPosition = rb.position + new Vector3(currentVelocity.x, 0, 0); // Z component is zero

        // Calculate the direction from the anchor point to the new position
        Vector3 directionToNewPosition = newPosition - anchorPoint;
        directionToNewPosition.y = 0; // Ignore vertical component for horizontal constraints

        // Clamp the distance to the maximum range if necessary
        if (directionToNewPosition.magnitude > maxRange)
        {
            directionToNewPosition = directionToNewPosition.normalized * maxRange;
        }

        // Calculate the angle between the forward direction and the direction to the new position
        float newAngle = Vector3.SignedAngle(Vector3.forward, directionToNewPosition, Vector3.up);

        // Clamp the angle to the allowed range
        if (Mathf.Abs(newAngle) > maxAngle)
        {
            float clampedAngle = Mathf.Clamp(newAngle, -maxAngle, maxAngle);
            Quaternion rotation = Quaternion.Euler(0, clampedAngle, 0);
            directionToNewPosition = rotation * Vector3.forward * directionToNewPosition.magnitude;
        }

        // Update the player's position after clamping (X and Z axes only)
        newPosition = anchorPoint + directionToNewPosition;
        newPosition.y = rb.position.y; // Keep the current Y position unchanged
        newPosition.z = rb.position.z; // Ensure Z position stays the same

        // Apply the new position to the Rigidbody
        rb.MovePosition(newPosition);

        // Handle vertical physics (gravity, jumping)
        rb.velocity = new Vector3(currentVelocity.x, verticalVelocity, 0); // Ensure Z velocity remains zero

        // After the player jumps, they'll dive below the surface after they hit the min height, and start going back up
        if (rb.position.y < minHeight - 0.1f && (state == fishState.JUMPING || state == fishState.DIVING))
        {
            rb.AddForce(Vector3.up * buoyancy, ForceMode.Acceleration);
            state = fishState.DIVING;
        }
        else if (rb.position.y >= minHeight && state == fishState.DIVING) // Stop vertical movement when surfacing
        {
            rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z); // Stop upward movement
            state = fishState.SURFACE;
        }
        else if (state == fishState.SURFACE) // Keep vertical movement steady when on the surface
        {
            Vector3 correctedPosition = rb.position;
            correctedPosition.y = minHeight;
            rb.position = correctedPosition;

            rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z); // Stop downward movement
        }

        //Debug.Log(rb.position + " " + state);
    }

    // Function to make the player jump
    private void Jump()
    {
        // Only apply a fixed upward force for the jump
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        state = fishState.JUMPING;
    }

    private float getAnchorDist()
    {
        distFromAnchor.x = transform.position.x - anchorPoint.x;
        distFromAnchor.y = transform.position.y - anchorPoint.y;

        return distFromAnchor.magnitude;
    }


    // This function will help visualize the anchor point and movement vector in the editor
    private void OnDrawGizmos()
    {
        // Draw the anchor point in the scene view
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(anchorPoint, 0.2f);  // Visualize the anchor point

        // Draw the max range
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(anchorPoint, maxRange); // Visualize the max range

        // Draw the allowed movement angle
        Vector3 leftLimit = Quaternion.Euler(0, -maxAngle, 0) * Vector3.forward * maxRange;
        Vector3 rightLimit = Quaternion.Euler(0, maxAngle, 0) * Vector3.forward * maxRange;

        Gizmos.color = Color.green;
        Gizmos.DrawLine(anchorPoint, anchorPoint + leftLimit);  // Left constraint
        Gizmos.DrawLine(anchorPoint, anchorPoint + rightLimit); // Right constraint
    }
}
