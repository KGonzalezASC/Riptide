using System;
using System.Collections;
using System.Collections.Generic;
//using System.Diagnostics;
using System.Xml;
using UnityEngine;
using UnityEngine.Assertions.Must;
using UnityEngine.InputSystem;

public enum FishMovementState
{
    SURFACE,
    JUMPING,
    DIVING,
    GRINDING
}

public enum FishPowerUpState
{
    NONE,
    BOTTLEBREAKER,
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
    private float jumpForce = 5.8f; // The force applied when jumping
    private float activeJumpForce;

    [SerializeField]
    private float maxUnderwaterSpeed = 2.0f;

    [SerializeField]
    private float maxLateralSpeed = 5.0f;

    [SerializeField]
    private float surfaceAlignmentForce = -20f;

    [SerializeField]
    private float buoyancy = 40f;

    [SerializeField]
    private float minHeight = 1f; // Define the minimum height

    private float grindHeight = 0.0f; // Used for storing the height to maintain when grinding
    private float grindSnapX = 0.0f;
    private bool perfectDismountReady = false;
    private bool bounceReady = false;

    private Vector2 moveDirection = Vector2.zero;

    [SerializeField]
    private FishMovementState state = FishMovementState.SURFACE;
 
    public FishPowerUpState powerUpState = FishPowerUpState.NONE;

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

    private bool isGrounded;
    private bool canJump;

    private ScoreTracker scoreTracker;

    private void OnEnable()
    {
        playerControls.Enable();
        jumpAction.Enable();
        transform.Rotate(0, 180, 0);
        state = FishMovementState.SURFACE;

        if (scoreTracker == null && GameObject.FindWithTag("UI") != null)
        {
            scoreTracker = GameObject.FindWithTag("UI").GetComponent<ScoreTracker>();
            //Debug.Log("Player found score tracker");
        }
        activeJumpForce = jumpForce;
    }

    private void OnDisable()
    {
        playerControls.Disable();
        jumpAction.Disable();
    }

    private void Update()
    {
        if (GameManager.instance.topState.GetName() == "Game")
        {
            moveDirection = playerControls.ReadValue<Vector2>();
            if (jumpAction.triggered && state != FishMovementState.JUMPING && state != FishMovementState.DIVING)
            {
                if (perfectDismountReady && state == FishMovementState.GRINDING)
                {
                    //Debug.Log("Perfect dismount!");
                    scoreTracker.buildTrickMultiplier(1.5f);
                }

                Jump();
            }
            else if (state == FishMovementState.JUMPING && bounceReady)
            {
                hazardBounce();
                scoreTracker.buildTrickScore(100);
                Debug.Log("Player successfully performed a hazard bounce");
            }
        }

        rb.rotation = Quaternion.Euler(0f, -180f, 0f);
        transform.rotation = Quaternion.Euler(0f, -180f, 0f);

        transform.position.Set(transform.position.x, transform.position.y, 0);
        rb.position.Set(rb.position.x, rb.position.y, 0);
    }

    private void FixedUpdate()
    {
        // Get the current velocity
        Vector3 currentVelocity = rb.velocity;

        // Preserve the current vertical velocity (Y) so jumping and gravity aren't overwritten
        float verticalVelocity = currentVelocity.y;

        // Apply horizontal movement based on input
        if (moveDirection != Vector2.zero && state != FishMovementState.GRINDING)
        {
            // Smoothly interpolate the horizontal movement
            float targetXVelocity = moveDirection.x * movementSpeed * Time.deltaTime;
            currentVelocity.x = Mathf.Lerp(currentVelocity.x, targetXVelocity, friction);
        }
        else if (state == FishMovementState.SURFACE || state == FishMovementState.DIVING) // Apply gradual slow-down when grounded and no input
        {
            // Gradually reduce horizontal velocity using friction
            // This line was the reason for the pull towards the middle,
            // and the movement seems to work alright without it. May want to revisit, though
            //currentVelocity.x = Mathf.Lerp(currentVelocity.x, 0, friction);
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

        if (state == FishMovementState.GRINDING)
        {
            newPosition.x = grindSnapX;
        }

        newPosition.y = rb.position.y; // Keep the current Y position unchanged
        newPosition.z = rb.position.z; // Ensure Z position stays the same

        // Apply the new position to the Rigidbody
        rb.MovePosition(newPosition);

        // Handle vertical physics (gravity, jumping)
        rb.velocity = new Vector3(currentVelocity.x, verticalVelocity, 0); // Ensure Z velocity remains zero

        // After the player jumps, they'll dive below the surface after they hit the min height, and start going back up
        if (rb.position.y < minHeight - 0.1f && (state == FishMovementState.JUMPING || state == FishMovementState.DIVING))
        {
            rb.AddForce(Vector3.up * buoyancy, ForceMode.Acceleration);
            state = FishMovementState.DIVING;

            scoreTracker.gainTrickScore(false);
        }
        else if (rb.position.y >= minHeight && state == FishMovementState.DIVING) // Stop vertical movement when surfacing
        {
            //rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z); // Stop upward movement
            state = FishMovementState.SURFACE;
            NormalJump();
        }
        else if (state == FishMovementState.SURFACE) // Keep vertical movement steady at minHeight when on the surface
        {
            rb.rotation = Quaternion.Euler(0f, -180f, 0f);
            //Vector3 correctedPosition = rb.position;
            //correctedPosition.y = minHeight;
            //rb.position = correctedPosition;

            rb.AddForce(Vector3.up * surfaceAlignmentForce * (rb.position.y - minHeight), ForceMode.Acceleration); // correction force
            if (rb.position.y < minHeight)
            {
                rb.useGravity = false;
                rb.position = new Vector3(rb.position.x, minHeight, rb.position.z);
            }
        }
        else if (state == FishMovementState.GRINDING) // Keep vertical movement steady at grindHeight when grinding
        {
            Vector3 correctedPosition = rb.position;
            correctedPosition.y = grindHeight;
            rb.position = correctedPosition;

            rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z); // Stop downward movement

            scoreTracker.buildTrickScore(5);
        }

        if (state == FishMovementState.DIVING && rb.velocity.y > maxUnderwaterSpeed)
        {
            rb.velocity = new Vector3(rb.velocity.x, maxUnderwaterSpeed, rb.velocity.z);
        }

        if (state == FishMovementState.JUMPING || state == FishMovementState.DIVING)
        {
            rb.useGravity = true;
        }

        if (rb.velocity.x > Math.Pow(maxLateralSpeed, 2))
        {
            rb.velocity.Set(maxLateralSpeed, rb.velocity.y, rb.velocity.z);
        }
        if (rb.velocity.x < 0f - Math.Pow(maxLateralSpeed, 2))
        {
            rb.velocity.Set(0 - maxLateralSpeed, rb.velocity.y, rb.velocity.z);
        }


        //Debug.Log(rb.position + " " + state);
    }

    // Function to make the player jump
    private void Jump()
    {
        // Only apply a fixed upward force for the jump
        rb.velocity = new Vector3(rb.velocity.x, activeJumpForce, rb.velocity.z);
        state = FishMovementState.JUMPING;
    }

    private float getAnchorDist()
    {
        distFromAnchor.x = transform.position.x - anchorPoint.x;
        distFromAnchor.y = transform.position.y - anchorPoint.y;

        return distFromAnchor.magnitude;
    }

    public void startGrind(float snapXTo, float snapYTo)
    {
        if (state != FishMovementState.GRINDING)
        {
            grindSnapX = snapXTo;
            grindHeight = snapYTo;

            rb.position = new Vector3(snapXTo, grindHeight, rb.position.z);
            state = FishMovementState.GRINDING;
            //Debug.Log("Started grinding, x snap loc is: " + snapXTo);
        }
    }

    public void stopGrind()
    {
        state = FishMovementState.JUMPING;

        if (perfectDismountReady)
        {
            perfectDismountReady = false;
        }
    }

    public void preparePerfectDismount()
    {
        perfectDismountReady = true;
        SuperJump();

    }

    public void setHazardBounceReady(bool value)
    {
        bounceReady = value;
    }

    private void hazardBounce()
    {
        // Apply a smaller fixed upward force for a hazard bounce
        rb.velocity = new Vector3(rb.velocity.x, activeJumpForce / 4, rb.velocity.z);
    }

    public void resetState()
    {
        state = FishMovementState.SURFACE;
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

    public IEnumerator PowerupTime(float delay)
    {
        //set powerup state only if powerup state is none
        if (powerUpState == FishPowerUpState.NONE)
        {
            powerUpState = FishPowerUpState.BOTTLEBREAKER;
            yield return Helpers.GetWaitForSeconds(delay);
            powerUpState = FishPowerUpState.NONE;
            Debug.Log("Powerup time ended");
        }
        else
        {
            Debug.Log("Powerup time already active");
        }
    }

    public void OnFishDeath() {
        stopGrind();
        state = FishMovementState.JUMPING;
        powerUpState = FishPowerUpState.NONE;
    }

    //super jump
    public void SuperJump()
    {
        activeJumpForce = jumpForce * 1.2f;
    }

    //normal jump
    public void NormalJump()
    {
        activeJumpForce = jumpForce;
    }

}