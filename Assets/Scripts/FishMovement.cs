using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Xml;
using UnityEngine;
using UnityEngine.Assertions.Must;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public enum FishMovementState
{
    SURFACE,
    JUMPING,
    DIVING,
    GRINDING,
    TRICK
}

public enum FishPowerUpState
{
    NONE,
    BOTTLEBREAKER,
}

public class FishMovement : MonoBehaviour
{
    [Header("Movement Values")]
    [SerializeField]
    private InputAction playerControls; // Input for Movement

    [SerializeField]
    private InputAction jumpAction; // Input for jumping
    
    [SerializeField]
    private InputAction trickControls; // Input for tricks

    [SerializeField]
    private float movementSpeed = 100f;

    [SerializeField]
    private float jumpForce = 5.8f; // The force applied when jumping

    [SerializeField]
    private float maxUnderwaterSpeed = 2.0f;

    [SerializeField]
    private float maxLateralSpeed = 5.0f;

    [SerializeField]
    private float surfaceAlignmentForce = -20f;

    [SerializeField]
    private const float baseBouyancy = 40f;
    private float buoyancy = baseBouyancy;

    [SerializeField]
    private float minHeight = 1f; // Define the minimum height
    [SerializeField]
    private float maxDiveDepth = -10f;  // Adjust based on the maximum dive depth you want

    [SerializeField]
    private float friction = 0.05f; // Friction factor for slowing down

    [SerializeField]
    private Vector3 anchorPoint = Vector3.zero;  // Anchor point to rotate around


    [Header("Movement Constraints")]
    [SerializeField]
    private float maxAngle = 45f;  // Maximum angle (in degrees) left/right from anchor point
    [SerializeField]
    private float maxRange = 5f;   // Maximum distance from anchor point


    [Header("Misc")]

    [SerializeField]
    private FishMovementState movementState = FishMovementState.SURFACE;
    public FishPowerUpState powerUpState = FishPowerUpState.NONE;
    [SerializeField]
    private Rigidbody rb;
    [SerializeField]  //global volume ref
    private Volume volume;
    [SerializeField]
    private float hazardCheckDistance = 8.0f;
    [SerializeField]
    private ParticleSystem splash;




    [Header("Not Serialized")]
    private bool isGrounded;
    private bool canJump;
    private ScoreTracker scoreTracker;
    private float grindHeight = 0.0f; // Used for storing the height to maintain when grinding
    private float grindSnapX = 0.0f;
    private Vector3 grindDir = new Vector3(0, 0, 0);
    private bool perfectDismountReady = false;
    private bool bounceReady = false;
    private int hazardBounceCounter = 0;
    private Vector2 moveDirection = Vector2.zero; private Vector2 trickDirection = Vector2.zero;
    private Vector3 spinDir = Vector3.zero;
    private float spinSpeed = 720.0f;
    private float trickTimer = 0.0f;
    private int trickCounter = 0;
    private float activeJumpForce;
    private Vector2 distFromAnchor;


    private void OnEnable()
    {
        playerControls.Enable();
        jumpAction.Enable();
        trickControls.Enable();

        resetState();

        if (scoreTracker == null && GameObject.FindWithTag("UI") != null)
        {
            scoreTracker = GameObject.FindWithTag("UI").GetComponent<ScoreTracker>();
        }
        //find global volume in scene
        if (volume == null)
        {
            volume = GameObject.FindObjectOfType<Volume>();
        }
        activeJumpForce = jumpForce;
    }

    private void OnDisable()
    {
        playerControls.Disable();
        jumpAction.Disable();
        trickControls.Disable();
    }

    private void Update()
    {
        if (GameManager.instance.topState.GetName() == "Game")
        {
            HandleJumpActions();

            HandleTrickActions();

            updateRotations();
        }
    }

    private void FixedUpdate()
    {
        if (GameManager.instance.topState.GetName() == "Game")
        {
            Vector3 currentVelocity = rb.velocity;

            currentVelocity = HandleHorizontalMovement(currentVelocity);

            currentVelocity = HandleVerticalMovement(currentVelocity);

            HandleSpin();
        }
    }

    private void HandleJumpActions()
    {

        if (jumpAction.triggered && (movementState != FishMovementState.JUMPING && movementState != FishMovementState.TRICK))
        {
            if (perfectDismountReady && movementState == FishMovementState.GRINDING)
            {
                //Debug.Log("Perfect dismount!");
                scoreTracker.buildTrickMultiplier(0.5f);
            }

            // If underwater and jump is pressed check for depth in some way
            if (movementState == FishMovementState.DIVING)
            {
                float currentDepth = rb.position.y;
                float diveRange = minHeight - maxDiveDepth;
                float depthPercentage = (minHeight - currentDepth) / diveRange;

                // Allow jump cancel if within the first 20% of the dive depth range
                if (depthPercentage <= 0.2f)
                {
                    // Zero out vertical forces and reset height
                    rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);  // Set vertical velocity to 0
                    rb.position = new Vector3(rb.position.x, minHeight, rb.position.z);  // Reset height
                    activeJumpForce = jumpForce;  // Reset jump force
                }
            }

            Jump();
        }
        else if (bounceReady && jumpAction.triggered && movementState == FishMovementState.JUMPING)
        {
            hazardBounce();
            scoreTracker.buildTrickScore(100);

            if (hazardBounceCounter >= 1)
            {
                scoreTracker.buildTrickMultiplier(0.1f);
            }

            hazardBounceCounter++;
        }
    }

    private void HandleTrickActions()
    {
        trickDirection = trickControls.ReadValue<Vector2>();

        //UnityEngine.Debug.Log("trickdir x: " + trickDirection.x + ", trickdir y: " + trickDirection.y);

        if (movementState == FishMovementState.JUMPING && trickDirection != Vector2.zero)
        {
            bool firstTrick = false;

            if (trickCounter == 0)
            {
                firstTrick = true;
            }

            if (trickDirection.y > 0)
            {
                startTrick(1, firstTrick);
            }
            else if (trickDirection.y < 0)
            {
                startTrick(2, firstTrick);
            }
            else if (trickDirection.x > 0)
            {
                startTrick(3, firstTrick);
            }
            else if (trickDirection.x < 0)
            {
                startTrick(4, firstTrick);
            }
        }
    }

    public void Jump()
    {
        // Only apply a fixed upward force for the jump
        rb.velocity = new Vector3(rb.velocity.x, activeJumpForce, rb.velocity.z);
        movementState = FishMovementState.JUMPING;
        if (perfectDismountReady)
        {
            StartCoroutine(FishAscension());
        }
    }

    //Demo jump call jump if grounded
    public void DemoJump()
    {
        //if on surface jump
        if (movementState == FishMovementState.SURFACE || movementState == FishMovementState.GRINDING)
        {
            Jump();
        }
    }

    private void updateRotations()
    {
        if (gameObject.name == "FishBoard(Clone)" && movementState != FishMovementState.TRICK)
        {
            rb.rotation = Quaternion.Euler(0f, -180f, 0f);
            transform.rotation = Quaternion.Euler(0f, -180f, 0f);
        }

        transform.position.Set(transform.position.x, transform.position.y, 0);
        rb.position.Set(rb.position.x, rb.position.y, 0);
    }

    private Vector3 HandleHorizontalMovement(Vector3 currentVelocity)
    {
        moveDirection = playerControls.ReadValue<Vector2>();

        Vector3 newPosition = rb.position;

        if (movementState != FishMovementState.GRINDING)
        {
            float currentXDir = rb.velocity.x / MathF.Abs(rb.velocity.x);

            // Apply horizontal movement based on input
            if (moveDirection != Vector2.zero)
            {
                // Smoothly interpolate the horizontal movement
                float targetXVelocity = moveDirection.x * movementSpeed * Time.deltaTime;
                //currentVelocity.x = Mathf.Lerp(currentVelocity.x, targetXVelocity, friction);

                float acceleration = 0.3f;

                rb.AddForce(new Vector3(acceleration * moveDirection.x, 0, 0), ForceMode.Acceleration);
            }
            else
            {
                if (rb.velocity.x <= 0.05f)
                {
                    rb.velocity.Set(0, rb.velocity.y, rb.velocity.z);
                }

                if (currentXDir != 0)
                {
                    rb.AddForce(new Vector3(-currentXDir * friction, 0, 0), ForceMode.Acceleration);
                }

            }

            if (MathF.Abs(rb.velocity.x) > movementSpeed)
            {
                rb.velocity.Set(currentXDir * movementSpeed, rb.velocity.y, rb.velocity.z);
            }

            //rb.velocity.Set(currentVelocity.x, rb.velocity.y, rb.velocity.z);

            // Calculate new position after applying horizontal velocity
            newPosition.x += rb.velocity.x;

            //// Calculate the direction from the anchor point to the new position
            //Vector3 directionToNewPosition = newPosition - anchorPoint;
            //directionToNewPosition.y = 0; // Ignore vertical component for horizontal constraints

            //// Clamp the distance to the maximum range if necessary
            //if (directionToNewPosition.magnitude > maxRange)
            //{
            //    directionToNewPosition = directionToNewPosition.normalized * maxRange;
            //}

            //// Calculate the angle between the forward direction and the direction to the new position
            //float newAngle = Vector3.SignedAngle(Vector3.forward, directionToNewPosition, Vector3.up);

            //// Clamp the angle to the allowed range
            //if (Mathf.Abs(newAngle) > maxAngle)
            //{
            //    float clampedAngle = Mathf.Clamp(newAngle, -maxAngle, maxAngle);
            //    Quaternion rotation = Quaternion.Euler(0, clampedAngle, 0);
            //    directionToNewPosition = rotation * Vector3.forward * directionToNewPosition.magnitude;
            //}

            //// Update the player's position after clamping (X and Z axes only)
            //newPosition = anchorPoint + directionToNewPosition;
        }
        else
        {
            newPosition.x = grindSnapX;
        }

        newPosition.y = rb.position.y; // Keep the current Y position unchanged
        newPosition.z = rb.position.z; // Ensure Z position stays the same

        // Apply the new position to the Rigidbody
        rb.MovePosition(newPosition);


        if (rb.velocity.x > Math.Pow(maxLateralSpeed, 2))
        {
            rb.velocity.Set(maxLateralSpeed, rb.velocity.y, rb.velocity.z);
        }
        if (rb.velocity.x < 0f - Math.Pow(maxLateralSpeed, 2))
        {
            rb.velocity.Set(0 - maxLateralSpeed, rb.velocity.y, rb.velocity.z);
        }

        return rb.velocity;
    }

    private Vector3 HandleVerticalMovement(Vector3 currentVelocity)
    {
        float verticalVelocity = currentVelocity.y;

        currentVelocity.z = 0;

        switch (movementState)
        {
            case FishMovementState.DIVING:

                if (rb.position.y >= minHeight) // Stop vertical movement when surfacing
                {
                    movementState = FishMovementState.SURFACE;
                    NormalJump();
                }

                if (rb.velocity.y > maxUnderwaterSpeed) // Cap underwater speed
                {
                    rb.velocity = new Vector3(rb.velocity.x, maxUnderwaterSpeed, rb.velocity.z);
                }

                rb.useGravity = true;

                // Check for a landing
                if (rb.position.y < minHeight - 0.1f)
                {
                    BottleImpact();

                    if (scoreTracker != null)
                    {
                        if (movementState == FishMovementState.JUMPING)
                        {
                            scoreTracker.gainTrickScore(false);
                        }
                        else if (movementState == FishMovementState.TRICK)
                        {
                            scoreTracker.loseTrickScore();
                        }

                        hazardBounceCounter = 0;
                        perfectDismountReady = false;
                        trickCounter = 0;
                    }

                    rb.AddForce(Vector3.up * buoyancy, ForceMode.Acceleration);
                    movementState = FishMovementState.DIVING;
                    buoyancy = baseBouyancy; // Ensure bouyancy is reset
                }

                break;
            case FishMovementState.JUMPING:

                rb.useGravity = true;

                // Check for a landing
                if (rb.position.y < minHeight - 0.1f)
                {
                    BottleImpact();

                    if (scoreTracker != null)
                    {
                        if (movementState == FishMovementState.JUMPING)
                        {
                            scoreTracker.gainTrickScore(false);
                        }
                        else if (movementState == FishMovementState.TRICK)
                        {
                            scoreTracker.loseTrickScore();
                        }

                        hazardBounceCounter = 0;
                        perfectDismountReady = false;
                        trickCounter = 0;
                    }

                    rb.AddForce(Vector3.up * buoyancy, ForceMode.Acceleration);
                    movementState = FishMovementState.DIVING;
                    buoyancy = baseBouyancy; // Ensure bouyancy is reset
                }

                break;
            case FishMovementState.TRICK:

                rb.useGravity = true;

                // Check for a landing
                if (rb.position.y < minHeight - 0.1f)
                {
                    BottleImpact();

                    if (scoreTracker != null)
                    {
                        if (movementState == FishMovementState.JUMPING)
                        {
                            scoreTracker.gainTrickScore(false);
                        }
                        else if (movementState == FishMovementState.TRICK)
                        {
                            scoreTracker.loseTrickScore();
                        }

                        hazardBounceCounter = 0;
                        perfectDismountReady = false;
                        trickCounter = 0;
                    }

                    rb.AddForce(Vector3.up * buoyancy, ForceMode.Acceleration);
                    movementState = FishMovementState.DIVING;
                    buoyancy = baseBouyancy; // Ensure bouyancy is reset
                }

                break;
            case FishMovementState.SURFACE:
                
                //if tag is player
                if (gameObject.name == "FishBoard(Clone)")
                {
                    rb.rotation = Quaternion.Euler(0f, -180f, 0f);
                }

                rb.AddForce(Vector3.up * surfaceAlignmentForce * (rb.position.y - minHeight), ForceMode.Acceleration); // correction force
                if (rb.position.y < minHeight)
                {
                    rb.useGravity = false;
                    rb.position = new Vector3(rb.position.x, minHeight, rb.position.z);
                }

                break;
            case FishMovementState.GRINDING:
                Vector3 correctedPosition = rb.position;
                correctedPosition.y = grindHeight;
                grindHeight += grindDir.y * Time.deltaTime;
                rb.position = correctedPosition;

                rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z); // Stop downward movement
                if (scoreTracker != null)
                    scoreTracker.buildTrickScore(5);

                break;
        }

        rb.velocity.Set(currentVelocity.x, currentVelocity.y, currentVelocity.z);

        return currentVelocity;
    }

    private void HandleSpin()
    {
        if (movementState == FishMovementState.TRICK)
        {
            Vector3 spin = spinDir * spinSpeed;

            Quaternion deltaRotation = Quaternion.Euler(spin * Time.deltaTime);

            rb.MoveRotation(rb.rotation * deltaRotation);

            trickTimer += Time.deltaTime;

            if (trickTimer >= 0.5f)
            {
                completeTrick();
            }
        }
    }

    private float getAnchorDist()
    {
        distFromAnchor.x = transform.position.x - anchorPoint.x;
        distFromAnchor.y = transform.position.y - anchorPoint.y;

        return distFromAnchor.magnitude;
    }

    public void startGrind(float snapXTo, float snapYTo, Vector3 moveDir)
    {
        if (movementState != FishMovementState.GRINDING)
        {
            grindSnapX = snapXTo;
            grindDir = moveDir;

            if (snapYTo != -1.0f)
            {
                grindHeight = snapYTo;
            }
            else
            {
                grindHeight = rb.position.y;
            }
            //rotate player.rotation.y to match to grind direction.y
            grindDir.y = moveDir.y;
            //rotate the y direction to match the grind direction
            transform.rotation = Quaternion.Euler(0, grindDir.y, 0);

            rb.position = new Vector3(snapXTo, grindHeight, rb.position.z);
            movementState = FishMovementState.GRINDING;
            //Debug.Log("Started grinding, x snap loc is: " + snapXTo);
        }
    }

    public void stopGrind()
    {
        movementState = FishMovementState.JUMPING;

        if (perfectDismountReady)
        {
            perfectDismountReady = false;
        }
    }

    public void BottleImpact() {
        if (movementState != FishMovementState.DIVING)
        {
            Instantiate(splash, rb.position + new Vector3(0f, 0.5f, -1.20f), Quaternion.identity);
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

    public void hazardBounce()
    {
        // Apply a smaller fixed upward force for a hazard bounce 
        rb.velocity = new Vector3(rb.velocity.x, activeJumpForce / 1.325f, rb.velocity.z);
        setHazardBounceReady(false);
    }

    private void startTrick(int direction, bool first)
    {
        //check is player is atleast .3 above min height before tricking
        if(rb.position.y < minHeight + 0.3f)
        {
            return;
        }
        //max height to prevent infinite jump... //doesnt solve jumping from grind rail flat or angle early and jumping super high
        if (rb.position.y > 7)
        {
            return;
        }

        if (first)
        {
            rb.velocity = new Vector3(rb.velocity.x, activeJumpForce / 2.2f, rb.velocity.z);
        }

        switch (direction)
        {
            case 1:
                UnityEngine.Debug.Log("Front Flip");
                spinDir.x = -1;
                spinDir.y = 0;
                spinDir.z = 0;
                break;
            case 2:
                UnityEngine.Debug.Log("Back Flip");
                spinDir.x = 1;
                spinDir.y = 0;
                spinDir.z = 0;
                break;
            case 3:
                UnityEngine.Debug.Log("Clockwise Barrel Roll");
                spinDir.x = 0;
                spinDir.y = 0;
                spinDir.z = 1;
                break;
            case 4:
                UnityEngine.Debug.Log("Counterclockwise Barrel Roll");
                spinDir.x = 0;
                spinDir.y = 0;
                spinDir.z = -1;
                break;
        }

        movementState = FishMovementState.TRICK;
    }


    public void DemoFlip() {
        //perform a front flip
        if (movementState == FishMovementState.JUMPING)
        {
            startTrick(1, true);
        }
    }



    private void completeTrick()
    {
        UnityEngine.Debug.Log("Trick done");

        scoreTracker.buildTrickScore(100);
        trickCounter++;

        trickTimer = 0.0f;

        if (trickCounter > 1)
        {
            scoreTracker.buildTrickMultiplier(0.1f);
        }

        rb.rotation = Quaternion.Euler(0f, -180f, 0f);
        transform.rotation = Quaternion.Euler(0f, -180f, 0f);

        spinDir = Vector2.zero;
        movementState = FishMovementState.JUMPING;
    }

    public void resetState()
    {
        movementState = FishMovementState.SURFACE;
    }

    public void SetFishState(FishMovementState state) {
        //set movement state
        this.movementState = state;
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
            UnityEngine.Debug.Log("Powerup time ended");
        }
        else
        {
            UnityEngine.Debug.Log("Powerup time already active");
        }
        //draw hazard check distance with line in front of fish
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, transform.position + (-transform.forward * hazardCheckDistance));

    }

    public void OnFishDeath()
    {
        stopGrind();
        NormalJump();
        movementState = FishMovementState.SURFACE;
        powerUpState = FishPowerUpState.NONE;
        hazardBounceCounter = 0;
        rb.velocity = Vector3.zero;
        scoreTracker.loseTrickScore();
    }

    //super jump
    public void SuperJump()
    {
        //increase jump force
        activeJumpForce = jumpForce * 1.33f;
        //decrease bouyancy for slower rise
        buoyancy *= .56f;
    }

    //normal jump
    public void NormalJump()
    {
        activeJumpForce = jumpForce;
        buoyancy = baseBouyancy;
    }

    public void CheckForHazards()
    {
        //draw raycast mactching gizmos line and collecting length of objects hit
        RaycastHit[] hits = Physics.RaycastAll(transform.position, -transform.forward, hazardCheckDistance);
        UnityEngine.Debug.DrawRay(transform.position, -transform.forward * hazardCheckDistance, Color.yellow);
        //check if any of the objects hit are Hazard objects
        if (hits.Length > 0)
        {
            foreach (RaycastHit hit in hits)
            {
                if (hit.collider.name == "Hazard")
                {
                    var playState = GameManager.instance.topState as PlayState;
                    playState.ExtendTimer();
                    //exit out of loop if hazard is found
                    break;
                }
            }
        }


    }

    public IEnumerator FishAscension()
    {
        yield return Helpers.GetWaitForSeconds(.2f);

        if (volume.profile.TryGet(out Bloom bloomEffect))
        {
            bloomEffect.intensity.value = 1.1f;
            bloomEffect.dirtIntensity.value = 40;
        }

        if (volume.profile.TryGet(out ColorAdjustments colorAdjustments))
        {
            colorAdjustments.postExposure.value = 50.0f;
        }
        //reset bloom
        if (volume.profile.TryGet(out Bloom bloomEffect2))
        {
            //interpolate back to original values:
            float elapsed = 0f;
            float duration = 1.0f;
            while (elapsed < duration)
            {
                bloomEffect2.intensity.value = Mathf.Lerp(1.1f, 0.75f, elapsed / duration);
                bloomEffect2.dirtIntensity.value = Mathf.Lerp(40.0f, 0.0f, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }
        }
        StopCoroutine(FishAscension());
    }
}
