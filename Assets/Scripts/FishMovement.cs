using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Xml;
using UnityEngine;
using UnityEngine.Assertions.Must;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using static UnityEngine.ParticleSystem;
using Debug = UnityEngine.Debug;

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
    private InputAction quickfallControl; // Input for midair dive

    [SerializeField]
    private float maxHorizontalMoveSpeed = 100f;

    [SerializeField]
    private float horizontalAcceleration = 1.2f; // Acceleration for x movement

    [SerializeField]
    private float horizontalFriction = 0.05f; // Friction factor for slowing down

    [SerializeField]
    private float jumpForce = 5.8f; // The force applied when jumping

    [SerializeField]
    private float quickfallForce = 5.8f; // The force applied when midair diving

    [SerializeField]
    private float maxUnderwaterSpeed = 2.0f;

    [SerializeField]
    private float surfaceAlignmentForce = -20f;

    private const float baseBouyancy = 1.0f;
    private float buoyancy = baseBouyancy;

    [SerializeField]
    private float minHeight = 1f; // Define the minimum height
    [SerializeField]
    private float maxDiveDepth = -10f;  // Adjust based on the maximum dive depth you want

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
    [SerializeField]
    private ParticleSystem badJumpParticles; //for buffered jump..
    [SerializeField]
    private float jumpActionDelay = 0.3f; // Length of delay after jump during which tricks and dives can't be done



    [Header("Not Serialized")]
    private bool isGrounded;
    private bool canJump;
    private ScoreTracker scoreTracker;
    private float grindHeight = 0.0f; // Used for storing the height to maintain when grinding
    private float grindSnapX = 0.0f;
    private Vector3 grindDir = new(0, 0, 0);
    private bool perfectDismountReady = false;
    private bool bounceReady = false;
    private int hazardBounceCounter = 0;
    private Vector2 moveDirection = Vector2.zero;
    private Vector2 trickDirection = Vector2.zero;
    private Vector3 spinDir = Vector3.zero;
    private float spinSpeed = 720.0f;
    private float trickTimer = 0.0f;
    private int trickCounter = 0;
    private float activeJumpForce;
    private Vector2 distFromAnchor;
    private float lastReleaseTime = -1f; // Stores the time when the jump action was last released
    private bool hasBufferJumped = false;
    private float jumpActionDelayTimer = 0.0f;
    private bool quickfalling = false;

    private GameObject meshObject; // Child object with fish mesh


    private void OnEnable()
    {
        playerControls.Enable();
        jumpAction.Enable();
        jumpAction.canceled += OnJumpReleased; // Listen for the release of the button
        quickfallControl.Enable();
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

        if (gameObject.name == "FishBoard(Clone)" && transform.GetChild(0) != null)
        {
            meshObject = transform.GetChild(0).gameObject;
        }
    }

    private void OnJumpReleased(InputAction.CallbackContext context)
    {
        lastReleaseTime = Time.time; // Record the time of release
    }

    private void OnDisable()
    {
        playerControls.Disable();
        jumpAction.Disable();
        quickfallControl.Disable();
        trickControls.Disable();

        //clear jump release time
        lastReleaseTime = -1f;
        //clear canceled event
        jumpAction.canceled -= OnJumpReleased;
    }

    private void Update()
    {
        if (GameManager.instance.topState.GetName() == "Game")
        {
            Vector3 currentVelocity = rb.velocity;

            // Check for space bar input
            currentVelocity.y = HandleJumpActions(currentVelocity.y);

            // Check for arrow key input
            currentVelocity.y = HandleTrickActions(currentVelocity.y);

            currentVelocity.y = HandleDiveActions(currentVelocity.y);

            // Set rotations to default if not in a trick
            updateRotations();

            rb.velocity = currentVelocity;
        }
    }

    private void FixedUpdate()
    {
        if (GameManager.instance.topState.GetName() == "Game")
        {
            Vector3 currentVelocity = rb.velocity;

            currentVelocity.x = HandleHorizontalMovement(currentVelocity.x); // Handle A & D key movement

            currentVelocity.y = HandleVerticalMovement(currentVelocity.y); // Handle movement when jumping/doing tricks

            rb.velocity = currentVelocity;

            HandleSpin(); // Rotate if in a trick

            if (jumpActionDelayTimer > 0.0f)
            {
                jumpActionDelayTimer -= Time.deltaTime;
            }
        }
    }

    private float HandleJumpActions(float currentYVelocity)
    {
        // Jump, if in a state that allows it
        if (jumpAction.triggered && (movementState != FishMovementState.JUMPING && movementState != FishMovementState.TRICK))
        {
            jumpActionDelayTimer = jumpActionDelay;

            // Gain extra score on a perfect dismount from a rail
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
                    currentYVelocity = 0; ;  // Set vertical velocity to 0
                    rb.position = new Vector3(rb.position.x, minHeight, rb.position.z);  // Reset height
                    activeJumpForce = jumpForce;  // Reset jump force
                }
            }

            currentYVelocity = Jump(currentYVelocity);
        }
        // Check for a hazard bounce next
        else if (bounceReady && jumpAction.triggered && movementState == FishMovementState.JUMPING)
        {
            currentYVelocity = hazardBounce(currentYVelocity);

            // Adjust score and multiplier
            scoreTracker.buildTrickScore(100);

            if (hazardBounceCounter >= 1)
            {
                scoreTracker.buildTrickMultiplier(0.1f);
            }

            hazardBounceCounter++;
        }

        return currentYVelocity;
    }

    private float HandleTrickActions(float currentyVelocity)
    {
        // Get the direction in which to do the trick
        trickDirection = trickControls.ReadValue<Vector2>();

        //UnityEngine.Debug.Log("trickdir x: " + trickDirection.x + ", trickdir y: " + trickDirection.y);

        // Only do a trick when in jump state
        if (movementState == FishMovementState.JUMPING && trickDirection != Vector2.zero && jumpActionDelayTimer <= 0.0f)
        {
            if (trickCounter == 0)
            {
                currentyVelocity = jumpForce / 2.2f;
            }

            // Perform a trick based on direction
            if (trickDirection.y > 0)
            {
                startTrick(1);
            }
            else if (trickDirection.y < 0)
            {
                startTrick(2);
            }
            else if (trickDirection.x > 0)
            {
                startTrick(3);
            }
            else if (trickDirection.x < 0)
            {
                startTrick(4);
            }
        }

        return currentyVelocity;
    }

    private float HandleDiveActions(float currentyVelocity)
    {
        if (quickfallControl.triggered && (movementState == FishMovementState.JUMPING || movementState == FishMovementState.TRICK) && jumpActionDelayTimer <= 0.0f && !quickfalling)
        {
            quickfalling = true;
            
            currentyVelocity = Dive(currentyVelocity);
        }

        return currentyVelocity;
    }

    private float Dive(float currentYVelocity)
    {
        currentYVelocity -= quickfallForce;

        return currentYVelocity;
    }

    public float Jump(float currentYVelocity)
    {
        // Only apply a fixed upward force for the jump
        currentYVelocity += jumpForce;

        movementState = FishMovementState.JUMPING;

        if (perfectDismountReady)
        {
            StartCoroutine(FishAscension());
        }

        return currentYVelocity;
    }

    //Demo jump call jump if grounded
    public void DemoJump()
    {
        //if on surface jump
        if (movementState == FishMovementState.SURFACE || movementState == FishMovementState.GRINDING)
        {
            // Only apply a fixed upward force for the jump
            rb.velocity = new Vector3(rb.velocity.x, activeJumpForce, rb.velocity.z);
            movementState = FishMovementState.JUMPING;
            if (perfectDismountReady)
            {
                StartCoroutine(FishAscension());
            }
        }
    }

    public void DemoHazardBounce()
    {
        rb.velocity = new Vector3(rb.velocity.x, activeJumpForce / 1.325f, rb.velocity.z);

        buoyancy = baseBouyancy;

        setHazardBounceReady(false);
        hasBufferJumped = false;
    }

    // Set rotations to default (may be deprecated soon)
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

    private float HandleHorizontalMovement(float currentXVelocity)
    {
        // Get horizontal move direction
        moveDirection = playerControls.ReadValue<Vector2>();

        float currentXDir;

        // Only allow horizontal movement when not grinding
        if (movementState != FishMovementState.GRINDING)
        {
            // Get the current direction in which the fish is moving
            if (currentXVelocity != 0f)
            {
                currentXDir = currentXVelocity / MathF.Abs(currentXVelocity);
            }
            else
            {
                currentXDir = 0f;
            }

            // Apply horizontal movement based on input
            if (moveDirection != Vector2.zero)
            {
                float targetXVelocity = moveDirection.x * maxHorizontalMoveSpeed * Time.deltaTime;

                currentXVelocity += horizontalAcceleration * moveDirection.x; // Apply acceleration
            }
            else if (MathF.Abs(currentXVelocity) <= 0.4f)
            {
                currentXVelocity = 0.0f; // Set velocity to 0 when it's low enough
            }

            // Apply friction in the opposite direction of movement
            if (currentXDir != 0 && currentXVelocity != 0.0f)
            {
                float frictionAndDir = -currentXDir * horizontalFriction;

                currentXVelocity += frictionAndDir;
            }

            // Cap movement speed if it's too high
            if (MathF.Abs(rb.velocity.x) > maxHorizontalMoveSpeed)
            {
                currentXVelocity = currentXDir * maxHorizontalMoveSpeed;
            }

            // Contain the x position within specified boundaries
            if (Mathf.Abs(rb.position.x) > maxRange)
            {
                float side = 0.0f;

                if (rb.position.x > 0)
                {
                    side = 1.0f;
                }
                else
                {
                    side = -1.0f;
                }

                rb.MovePosition(new Vector3(maxRange * side, rb.position.y, rb.position.z));

                currentXVelocity = 0.0f;
            }
        }
        else
        {
            currentXVelocity = 0.0f; // No movement should occur on x axis when grinding
        }

        return currentXVelocity;
    }

    private float HandleVerticalMovement(float currentYVelocity)
    {
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
                    currentYVelocity = maxUnderwaterSpeed;
                }

                rb.useGravity = true;

                // Apply buoyancy
                if (rb.position.y < minHeight - 0.1f)
                {
                    BottleImpact();

                    currentYVelocity += buoyancy;
                    movementState = FishMovementState.DIVING;
                    buoyancy = baseBouyancy; // Ensure bouyancy is reset
                }

                quickfalling = false;

                setHazardBounceReady(false);

                break;
            case FishMovementState.JUMPING:

                rb.useGravity = true;

                // Check for a landing
                if (rb.position.y < minHeight - 0.1f)
                {
                    BottleImpact();

                    if (scoreTracker != null)
                    {
                        scoreTracker.gainTrickScore(false); // Score is awarded when landing while in this state

                        hazardBounceCounter = 0;
                        perfectDismountReady = false;
                        trickCounter = 0;
                    }

                    currentYVelocity += buoyancy;
                    movementState = FishMovementState.DIVING;
                    buoyancy = baseBouyancy; // Ensure bouyancy is reset

                    quickfalling = false;
                }

                HazardBounceBuffer();

                break;
            case FishMovementState.TRICK:

                rb.useGravity = true;

                // Check for a landing
                if (rb.position.y < minHeight - 0.1f)
                {
                    BottleImpact();

                    if (scoreTracker != null)
                    {
                        scoreTracker.loseTrickScore(); // Score is lost when landing while in this state

                        hazardBounceCounter = 0;
                        perfectDismountReady = false;
                        trickCounter = 0;
                    }

                    currentYVelocity += buoyancy;
                    movementState = FishMovementState.DIVING;
                    buoyancy = baseBouyancy; // Ensure buoyancy is reset

                    quickfalling = false;
                }

                setHazardBounceReady(false);

                break;
            case FishMovementState.SURFACE:

                // if tag is player
                if (gameObject.name == "FishBoard(Clone)")
                {
                    rb.rotation = Quaternion.Euler(0f, -180f, 0f);
                }

                currentYVelocity += surfaceAlignmentForce * (rb.position.y - minHeight); // correction force

                if (rb.position.y < minHeight)
                {
                    rb.useGravity = false;
                    rb.position = new Vector3(rb.position.x, minHeight, rb.position.z);
                }

                quickfalling = false;

                setHazardBounceReady(false);

                break;
            case FishMovementState.GRINDING:

                Vector3 correctedPosition = rb.position; // Control y position when grinding
                correctedPosition.y = grindHeight;

                grindHeight += grindDir.y * Time.deltaTime; // Apply grind rail slope if relevant

                rb.position = correctedPosition; // Adjust position

                currentYVelocity = 0f;

                if (scoreTracker != null)
                    scoreTracker.buildTrickScore(5);

                quickfalling = false;

                setHazardBounceReady(false);

                break;
        }

        //if y position is > 1.3 reset hasbufferjumped
        if (rb.position.y > 1.3f)
        {
            hasBufferJumped = false;
        }

        return currentYVelocity;
    }

    private void HandleSpin()
    {
        // Spin while in trick state
        if (movementState == FishMovementState.TRICK)
        {
            Vector3 spin = spinDir * spinSpeed;

            meshObject.transform.Rotate(spin * Time.deltaTime, Space.Self);

            trickTimer += Time.deltaTime;

            if (trickTimer >= 0.5f)
            {
                if (gameObject.name == "FishBoard(Clone)")
                {
                    completeTrick();
                }
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
            //reset bouyancy if not it stacks if you grind into grind rail
            buoyancy = baseBouyancy;

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

    public void BottleImpact()
    {
        if (movementState != FishMovementState.DIVING)
        {
            Instantiate(splash, rb.position + new Vector3(0f, 0.5f, -1.20f), Quaternion.identity);
        }
    }

    public void BufferJumpParticle()
    {
        Instantiate(badJumpParticles, rb.position + new Vector3(0f, 0.5f, -1.20f), Quaternion.identity);
        NormalJump();
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

    public float hazardBounce(float currentYVelocity)
    {
        // Apply a smaller fixed upward force for a hazard bounce
        currentYVelocity += (jumpForce / 1.1f);

        //also reset the bouyancy for predictable behaviour
        buoyancy = baseBouyancy;

        setHazardBounceReady(false);
        hasBufferJumped = false;

        return currentYVelocity;
    }

    private void startTrick(int direction)
    {
        //check is player is atleast .3 above min height before tricking
        if (rb.position.y < minHeight + 0.3f)
        {
            return;
        }
        //max height to prevent infinite jump... //doesnt solve jumping from grind rail flat or angle early and jumping super high
        if (rb.position.y > 7)
        {
            return;
        }

        // Determine spin direction
        switch (direction)
        {
            case 1:
                //UnityEngine.Debug.Log("Front Flip");
                spinDir.x = -1;
                spinDir.y = 0;
                spinDir.z = 0;
                break;
            case 2:
                //UnityEngine.Debug.Log("Back Flip");
                spinDir.x = 1;
                spinDir.y = 0;
                spinDir.z = 0;
                break;
            case 3:
                //UnityEngine.Debug.Log("Clockwise Barrel Roll");
                spinDir.x = 0;
                spinDir.y = 0;
                spinDir.z = 1;
                break;
            case 4:
                //UnityEngine.Debug.Log("Counterclockwise Barrel Roll");
                spinDir.x = 0;
                spinDir.y = 0;
                spinDir.z = -1;
                break;
        }

        movementState = FishMovementState.TRICK;
    }


    public void DemoFlip()
    {
        //perform a front flip
        if (movementState == FishMovementState.JUMPING)
        {
            StartCoroutine(DemoFlipDelay());
        }
    }


    // Apply score for completing a trick successfully
    private void completeTrick()
    {
        //UnityEngine.Debug.Log("Trick done");
        scoreTracker.buildTrickScore(100);
        trickCounter++;

        trickTimer = 0.0f;

        if (trickCounter > 1)
        {
            scoreTracker.buildTrickMultiplier(0.1f);
        }

        meshObject.transform.rotation = Quaternion.Euler(0f, -180f, 0f);

        spinDir = Vector2.zero;
        movementState = FishMovementState.JUMPING;
    }






    public void resetState()
    {
        movementState = FishMovementState.SURFACE;
    }

    public void SetFishState(FishMovementState state)
    {
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

        if (rb != null)
        {
            Gizmos.color = Color.cyan;

            // Calculate the box center and half extents based on your method
            Vector3 boxCenter = transform.position + Vector3.down * 0.5f; // Match the box center in HazardBounceBuffer
            Vector3 boxHalfExtents = new(0.15f, 0.3f, 2f);       // Match the box size

            // Draw the detection box
            Gizmos.matrix = Matrix4x4.TRS(boxCenter, transform.rotation, Vector3.one); // Apply object rotation
            Gizmos.DrawWireCube(Vector3.zero, boxHalfExtents * 2f); // Gizmos expect full size, not half extents

            // Reset Gizmos matrix to world coordinates
            Gizmos.matrix = Matrix4x4.identity;

            // Draw the forward direction line
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(boxCenter, boxCenter + -transform.forward * 2.5f); // Match the forward detection range

            // Visualize hit results (if in play mode or hitCount > 0)
            if (Application.isPlaying)
            {
                RaycastHit[] hits = new RaycastHit[3];
                int hitCount = Physics.BoxCastNonAlloc(
                    boxCenter,
                    boxHalfExtents,
                    -transform.forward,
                    hits,
                    Quaternion.identity,
                    2.5f
                );

                if (hitCount > 0)
                {
                    Gizmos.color = Color.red;
                    for (int i = 0; i < hitCount; i++)
                    {
                        // Draw a sphere at the hit points
                        Gizmos.DrawSphere(hits[i].point, 0.1f);
                    }
                }
            }
        }
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
        hasBufferJumped = false;
        powerUpState = FishPowerUpState.NONE;
        hazardBounceCounter = 0;
        rb.velocity = Vector3.zero;
        scoreTracker.loseTrickScore();
        buoyancy = baseBouyancy;
    }

    //super jump
    public void SuperJump()
    {
        //increase jump force
        activeJumpForce = jumpForce * 1.33f;
        //decrease bouyancy for slower rise, EDIT:// people did not like this in last couple playtest so it the effective swim up speed is now faster
        buoyancy *= 0.5f;
    }

    //normal jump
    public void NormalJump()
    {
        activeJumpForce = jumpForce;
    }

    public void CheckForHazards()
    {
        //draw raycast matching gizmos line and collecting length of objects hit
        RaycastHit[] hits = new RaycastHit[10]; // Adjust the size as needed
        int hitCount = Physics.RaycastNonAlloc(transform.position, -transform.forward, hits, hazardCheckDistance);
        UnityEngine.Debug.DrawRay(transform.position, -transform.forward * hazardCheckDistance, Color.yellow);
        //check if any of the objects hit are Hazard objects
        if (hitCount > 0)
        {
            for (int i = 0; i < hitCount; i++)
            {
                if (hits[i].collider.name == "Hazard")
                {
                    var playState = GameManager.instance.topState as PlayState;
                    playState.ExtendTimer();
                    //exit out of loop if hazard is found
                    break;
                }
            }
        }
    }


    public void HazardBounceBuffer()
    {
        Vector3 boxCenter = transform.position + Vector3.down * 0.5f;   // Adjust center for downward bias
        Vector3 boxHalfExtents = new(0.15f, 0.4f, 2f);      // Adjust dimensions for forward detection

        RaycastHit[] hits = new RaycastHit[3];
        int hitCount = Physics.BoxCastNonAlloc(
            boxCenter,
            boxHalfExtents,
            Vector3.back,         // prevent player rotation from affecting
            hits,
            Quaternion.identity,       // No rotation for simplicity
            2.5f                       // Forward range
        );

        UnityEngine.Debug.DrawLine(boxCenter, boxCenter + transform.forward * 2.0f, Color.yellow);

        if (hitCount > 0 && hasBufferJumped == false)
        {
            for (int i = 0; i < hitCount; i++)
            {
                if (hits[i].collider.gameObject.TryGetComponent<Hazard>(out Hazard hazard))
                {
                    if (hazard.settings.type == FlyWeightType.Hazard)
                    {
                        float timeSinceRelease = Time.time - lastReleaseTime;
                        if (hazard.getSpeed() > 20f && rb.velocity.y <= 0 && timeSinceRelease < .3f)
                        {
                            //to fast to detect buffer jump
                            Debug.Log("Too fast to buffer jump");
                            rb.velocity = new Vector3(rb.velocity.x, activeJumpForce / 1.45f, rb.velocity.z);
                            hasBufferJumped = true;
                            StartCoroutine(BufferJump());
                            break;
                        }

                        //print how the hit object was in its z velocity based on its transform component
                        if (rb.velocity.y < 0 && timeSinceRelease < .33f && rb.position.y < .97f)
                        {
                            rb.velocity = new Vector3(rb.velocity.x, activeJumpForce / 1.45f, rb.velocity.z);
                            hasBufferJumped = true;
                            StartCoroutine(BufferJump());
                        }
                    }
                }
            }
        }
    }


    //public getter for hasBufferJumped
    public bool getHasBufferJumped()
    {
        return hasBufferJumped;
    }

    public IEnumerator BufferJump()
    {
        //turn collision off to prevent death
        rb.detectCollisions = false;
        SFXManager.instance.playSFXClip(SFXManager.instance.bufferJumpSFX, transform, .2f);
        BufferJumpParticle();
        hasBufferJumped = false;
        yield return Helpers.GetWaitForSeconds(.07f);
        rb.detectCollisions = true;
        scoreTracker.buildTrickScore(-200);
        yield return null;
    }

    public IEnumerator DemoFlipDelay()
    {
        yield return Helpers.GetWaitForSeconds(.5f);
        //pick a random flip direction to call to start trick
        int flipDirection = UnityEngine.Random.Range(1, 3);
        startTrick(flipDirection);
        rb.velocity = new Vector3(rb.velocity.x, jumpForce / 2.2f, rb.velocity.z);

        yield return null;
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
