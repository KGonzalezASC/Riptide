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

    [SerializeField] private InputAction playerControls; // Input for Movement
    [SerializeField] private InputAction jumpAction; // Input for jumping
    [SerializeField] private InputAction trickControls; // Input for tricks
    [SerializeField] private InputAction quickfallControl; // Input for midair dive

    [SerializeField] private float horizontalAcceleration = 1.2f; // Acceleration for x movement
    [SerializeField] private float horizontalFriction = 0.05f; // Friction factor for slowing down

    [SerializeField] private float jumpForce = 5.8f; // The force applied when jumping
    [SerializeField] private float quickfallForce = 5.8f; // The force applied when midair diving

    [SerializeField] private float surfaceAlignmentForce = -20f;


    [Header("Movement Constraints")]

    [SerializeField] private float maxRange = 5f;   // Maximum distance from middle
    [SerializeField] private float minHeight = 1f; // Define the minimum height
    [SerializeField] private float maxDiveDepth = -10f;  // Adjust based on the maximum dive depth you want

    [SerializeField] private float maxHorizontalMoveSpeed = 100f;
    [SerializeField] private float maxUnderwaterSpeed = 2.0f;

    [SerializeField] private float jumpActionDelay = 0.3f; // Length of delay after jump during which tricks and dives can't be done


    [Header("Misc")]

    [SerializeField] private Rigidbody rb;

    [SerializeField] private Volume volume; //global volume ref
    
    [SerializeField] private float hazardCheckDistance = 8.0f;
    
    [SerializeField] private ParticleSystem splash;
    [SerializeField] private ParticleSystem badJumpParticles; //for buffered jump..


    [Header("Not Serialized")]

    private FishMovementState movementState = FishMovementState.SURFACE;
    public FishPowerUpState powerUpState = FishPowerUpState.NONE;

    private Vector2 moveDirection = Vector2.zero;
    private Vector2 trickDirection = Vector2.zero;
    private Vector3 spinDir = Vector3.zero;

    private ScoreTracker scoreTracker;
    private GameObject meshObject; // Child object with fish mesh

    private float grindHeight = 0.0f; // Used for storing the height to maintain when grinding
    private float grindSnapX = 0.0f;
    private Vector3 grindDir = new(0, 0, 0);

    private bool perfectDismountReady = false;
    private bool bounceReady = false;
    private int hazardBounceCounter = 0;
    private bool flipTargetHazardBounce = false;

    private float spinSpeed = 720.0f;
    private float trickTimer = 0.0f;
    private int trickCounter = 0;

    private float lastReleaseTime = -1f; // Stores the time when the jump action was last released
    private bool hasBufferJumped = false;

    private float jumpActionDelayTimer = 0.0f;
    private bool quickfalling = false;

    private const float baseBouyancy = 1.0f;
    private float buoyancy = baseBouyancy;

    #region Enable & Disable

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

        if (gameObject.name == "FishBoard(Clone)" && transform.GetChild(0) != null)
        {
            meshObject = transform.GetChild(0).gameObject;
        }
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

    #endregion

    #region Update Code

    private void Update()
    {
        if (GameManager.instance.topState.GetName() == "Game")
        {
            Vector3 currentVelocity = rb.velocity;

            // Check for space bar input
            currentVelocity.y = HandleJumpActions(currentVelocity.y);

            // Check for arrow key input
            currentVelocity.y = HandleTrickActions(currentVelocity.y);

            currentVelocity.y = HandleQuickfallActions(currentVelocity.y);

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

    #endregion

    #region Jumping

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
                    rb.position = new Vector3(rb.position.x, minHeight, rb.position.z);  // Reset height
                }
            }

            currentYVelocity = Jump(currentYVelocity, perfectDismountReady);
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

    public float Jump(float currentYVelocity, bool superJump)
    {
        if (!superJump)
        {
            currentYVelocity = jumpForce;
        }
        else
        {
            //increase jump force
            currentYVelocity = jumpForce * 1.33f;
            //decrease bouyancy for slower rise, EDIT:// people did not like this in last couple playtest so it the effective swim up speed is now faster
            buoyancy = 0.8f; //make solid value for consistent behaviour

            StartCoroutine(FishAscension());
        }

        perfectDismountReady = false;

        movementState = FishMovementState.JUMPING;

        return currentYVelocity;
    }

    //Demo jump call jump if grounded
    public void DemoJump()
    {
        //if on surface jump
        if (movementState == FishMovementState.SURFACE || movementState == FishMovementState.GRINDING)
        {
            // Only apply a fixed upward force for the jump
            rb.velocity = new Vector3(rb.velocity.x, jumpForce, rb.velocity.z);
            movementState = FishMovementState.JUMPING;
            if (perfectDismountReady)
            {
                StartCoroutine(FishAscension());
            }
        }
    }

    public void preparePerfectDismount()
    {
        perfectDismountReady = true;

    }

    #region Hazard Bounce

    public void setHazardBounceReady(bool value)
    {
        bounceReady = value;
    }

    public float hazardBounce(float currentYVelocity)
    {
        //Apply a smaller fixed upward force for a hazard bounce
        //adjusting to match similar version of before..
        currentYVelocity = jumpForce * 0.85f;
        //currentYVelocity += (jumpForce * 0.98f); /// when doing this consecutives bounces have falloff in height
        //this causes the hazardbuffer to be flagged as true due to y position causes it to apply a significant upwards force to the player model
        buoyancy = baseBouyancy;
        setHazardBounceReady(false);
        hasBufferJumped = false;
        flipTargetHazardBounce = false;

        return currentYVelocity;
    }

    public void promptFlipTargetHazardBounce()
    {
        flipTargetHazardBounce = true;
    }

    #region Buffer Jumping

    public float HazardBounceBuffer(float currentYVelocity)
    {
        Vector3 boxCenter = transform.position + Vector3.down * 0.5f;   // Adjust center for downward bias
        Vector3 boxHalfExtents = new(0.05f, 0.4f, 2f);      // Adjust dimensions for forward detection

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
                            currentYVelocity = hazardBounce(currentYVelocity);
                            hasBufferJumped = true;
                            StartCoroutine(BufferJump());
                            break;
                        }

                        //print how the hit object was in its z velocity based on its transform component
                        if (rb.velocity.y < 0 && timeSinceRelease < .33f && rb.position.y < .97f)
                        {
                            currentYVelocity = hazardBounce(currentYVelocity);
                            hasBufferJumped = true;
                            StartCoroutine(BufferJump());
                        }
                    }
                }
            }
        }

        return currentYVelocity;
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

    public void BufferJumpParticle()
    {
        Instantiate(badJumpParticles, rb.position + new Vector3(0f, 0.5f, -1.20f), Quaternion.identity);
    }

    #endregion

    #endregion

    private void OnJumpReleased(InputAction.CallbackContext context)
    {
        lastReleaseTime = Time.time; // Record the time of release
    }

    #region Demo Jump Actions



    public void DemoHazardBounce()
    {
        rb.velocity = new Vector3(rb.velocity.x, jumpForce / 1.325f, rb.velocity.z);
        setHazardBounceReady(false);
        hasBufferJumped = false;
    }

    #endregion

    #endregion

    #region Tricks

    private float HandleTrickActions(float currentyVelocity)
    {
        // Get the direction in which to do the trick
        trickDirection = trickControls.ReadValue<Vector2>();

        //UnityEngine.Debug.Log("trickdir x: " + trickDirection.x + ", trickdir y: " + trickDirection.y);

        // Only do a trick when in jump state
        if (movementState == FishMovementState.JUMPING && trickDirection != Vector2.zero && jumpActionDelayTimer <= 0.0f)
        {
            currentyVelocity = jumpForce * 0.2f;

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

    #region Demo Flip

    public void DemoFlip()
    {
        //perform a front flip
        if (movementState == FishMovementState.JUMPING)
        {
            StartCoroutine(DemoFlipDelay());
        }
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

    #endregion

    #endregion

    #region Quickfall

    private float HandleQuickfallActions(float currentyVelocity)
    {
        if (quickfallControl.triggered && (movementState == FishMovementState.JUMPING || movementState == FishMovementState.TRICK) && jumpActionDelayTimer <= 0.0f && !quickfalling)
        {
            quickfalling = true;
            
            currentyVelocity = Quickfall(currentyVelocity);
        }

        return currentyVelocity;
    }

    private float Quickfall(float currentYVelocity)
    {
        currentYVelocity = -quickfallForce;

        return currentYVelocity;
    }

    #endregion

    #region Movement and Rotations

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

                if (flipTargetHazardBounce)
                {
                    currentYVelocity = hazardBounce(currentYVelocity);
                }

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
                    //buoyancy = baseBouyancy; // Ensure bouyancy is reset

                    quickfalling = false;
                }

                currentYVelocity = HazardBounceBuffer(currentYVelocity);

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
                    //buoyancy = baseBouyancy; // Ensure buoyancy is reset

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
        else
        {
            meshObject.transform.rotation = Quaternion.Euler(0f, -180f, 0f);
        }
    }

    // Set rotations to default (may be deprecated soon)
    private void updateRotations()
    {
        if (gameObject.name == "FishBoard(Clone)" && movementState != FishMovementState.TRICK)
        {
            rb.rotation = Quaternion.Euler(0f, -180f, 0f);
        }
    }

    #region Grinding

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

    #endregion

    #endregion

    #region Misc

    public void resetState()
    {
        movementState = FishMovementState.SURFACE;
    }

    public void SetFishState(FishMovementState state)
    {
        //set movement state
        this.movementState = state;
    }

    //get fish state
    public FishMovementState GetFishState()
    {
        return movementState;
    }

    public void BottleImpact()
    {
        if (movementState != FishMovementState.DIVING)
        {
            Instantiate(splash, rb.position + new Vector3(0f, 0.5f, -1.20f), Quaternion.identity);
        }
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

    public void OnFishDeath()
    {
        stopGrind();
        movementState = FishMovementState.SURFACE;
        hasBufferJumped = false;
        powerUpState = FishPowerUpState.NONE;
        hazardBounceCounter = 0;
        rb.velocity = Vector3.zero;
        scoreTracker.loseTrickScore();
        buoyancy = baseBouyancy;
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

    #endregion
}