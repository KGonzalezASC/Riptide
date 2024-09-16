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
    private Vector2 movementInput;

    private void Awake()
    {
        // Initialize the input actions
        fishControls = new FishControls();
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
        Vector3 move = new Vector3(0f, movementInput.y, 0f) * speed * Time.deltaTime;
        transform.position += move;

        // Rotate left or right around the anchor point when moving horizontally
        if (movementInput.x != 0)
        {
            float direction = movementInput.x < 0 ? 1f: -1f;  // Determine rotation direction (left or right)
            transform.RotateAround(anchorPoint, Vector3.forward, direction * rotationSpeed * Time.deltaTime);
        }
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
