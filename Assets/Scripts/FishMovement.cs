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
    private FishControls fishControls;
    [SerializeField] private float speed = 5f;
    [SerializeField] private Vector3 anchorPoint = Vector3.zero;  // Anchor point to rotate around
    [SerializeField] private float rotationSpeed = 100f;           // Speed of rotation
    [SerializeField] private float gravityStrength = 0.8f;
    [SerializeField] private float jumpStrength = 200.0f;
    private float yVel = 0;
    private bool wentUnder = false;
    private bool canJump = true;
    private bool grinding = false;
    private Vector2 movementInput;
    private Vector2 distFromAnchor;

    private void Awake()
    {
        // Initialize the input actions
        fishControls = new FishControls();

        distFromAnchor = new Vector2(transform.position.x - anchorPoint.x, transform.position.y - anchorPoint.y);
    }

    private void OnEnable()
    {
        fishControls.Enable();
        fishControls.Fish.Move.performed += OnMovePerformed;
        fishControls.Fish.Move.canceled += OnMoveCanceled;
    }

    private void OnDisable()
    {
        // Unsubscribe from the input action events when disabled
        fishControls.Fish.Move.performed -= OnMovePerformed;
        fishControls.Fish.Move.canceled -= OnMoveCanceled;
        fishControls.Disable();
    }

    private void OnMovePerformed(InputAction.CallbackContext context)
    {
        movementInput = context.ReadValue<Vector2>();
    }

    private void OnMoveCanceled(InputAction.CallbackContext context)
    {
        movementInput = Vector2.zero;
    }

    private void Update()
    {
        // Move up and down directly
        //Vector3 move = new Vector3(0f, movementInput.y, 0f) * speed * Time.deltaTime;
        //transform.position += move;
        gravity();

        if (grinding)
        {
            yVel = 0;
        }

        transform.Translate(new Vector3(0, yVel * Time.deltaTime, 0));

        // Rotate left or right around the anchor point when moving horizontally
        if (movementInput.x != 0 && getAnchorDist() < 2.1f && getAnchorDist() > 1.9f)
        {
            float direction = movementInput.x < 0 ? 1f: -1f;  // Determine rotation direction (left or right)

            if (((direction == -1f && transform.position.x < 1.2f) || (direction == 1f && transform.position.x > -1.2f)) && !grinding)
            {
                transform.RotateAround(anchorPoint, Vector3.forward, direction * rotationSpeed * Time.deltaTime);
                transform.rotation = Quaternion.identity;
            }
        }
        else if (movementInput.x != 0 && (getAnchorDist() > 2.1f || getAnchorDist() < 1.9f))
        {
            float direction = movementInput.x < 0 ? 1f : -1f;  // Determine rotation direction (left or right)

            if (((direction == -1f && transform.position.x < 1.2f) || (direction == 1f && transform.position.x > -1.2f)) && !grinding)
            {
                transform.Translate(new Vector3(direction * -7f * Time.deltaTime * 0.7f, 0, 0));
            }
        }
        else if (grinding)
        {
            Debug.Log("Grinding");
        }

        if (movementInput.y > 0 && ((getAnchorDist() < 2.1f && getAnchorDist() > 1.9f && canJump) || grinding))
        {
            if (grinding)
            {
                stopGrind();
            }
            yVel += jumpStrength;
            canJump = false;
        }
    }

    private float getAnchorDist()
    {
        distFromAnchor.x = transform.position.x - anchorPoint.x;
        distFromAnchor.y = transform.position.y - anchorPoint.y;

        return distFromAnchor.magnitude;
    }

    private void gravity()
    {
        if (getAnchorDist() > 2.1f && distFromAnchor.y > 0 && !wentUnder)
        {
            yVel -= 9 * gravityStrength * Time.deltaTime;
        }
        else if (getAnchorDist() > 1.95f && wentUnder)
        {
            yVel = 0f;
            wentUnder = false;
            canJump = true;
        }
        else if (getAnchorDist() < 1.9f)
        {
            if (movementInput.y > 0)
            {
                yVel += 24 * gravityStrength * Time.deltaTime;
            }
            else
            {
                yVel += 12 * gravityStrength * Time.deltaTime;
            }

            wentUnder = true;
        }
    }

    private void snapXTo(float snapX)
    {
        transform.Translate(new Vector3(snapX - transform.position.x, 0, 0));
    }

    public void startGrind(float snapX)
    {
        snapXTo(snapX);
        grinding = true;
        canJump = true;
    }

    public void stopGrind()
    {
        grinding = false;
        Debug.Log("Stopped Grinding");
    }

    // This function will help visualize the anchor point and movement vector in the editor
    private void OnDrawGizmos()
    {
        // Draw the anchor point in the scene view
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(anchorPoint, 0.2f);  // Visualize the anchor point

        // Draw a line representing the movement direction in world space
        Gizmos.color = Color.blue;
        Vector3 movementDirection = new Vector3(movementInput.x, movementInput.y, 0f).normalized;
        Gizmos.DrawLine(transform.position, transform.position + movementDirection);
    }
}
