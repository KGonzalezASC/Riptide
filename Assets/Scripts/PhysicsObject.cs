using UnityEngine;

// Handles applying forces on object
public class PhysicsObject : MonoBehaviour
{
    [SerializeField]
    protected Vector3 position;
    protected Vector3 direction;
    [SerializeField]
    protected Vector3 velocity;
    [SerializeField]
    protected Vector3 acceleration;
    [SerializeField]
    protected Vector3 gravity;

    [SerializeField]
    protected float maxSpeed = 25;

    [SerializeField]
    protected float mass = 1;

    [SerializeField]
    protected float coeff;

    [SerializeField]
    protected bool useFriction;

    [SerializeField]
    protected bool useGravity;

    [SerializeField]
    protected float radius;

    [SerializeField]
    protected bool useBounce = true;

    /// <summary>
    /// Gets the velocity
    /// </summary>
    public Vector3 Velocity { get { return velocity; } }

    /// <summary>
    /// Gets the maximum speed of the object
    /// </summary>
    public float MaxSpeed { get { return maxSpeed; } }

    /// <summary>
    /// Gets the radius of the physics object
    /// </summary>
    public float Radius { get { return radius; } }

    // Start is called before the first frame update
    protected virtual void Start()
    {
        position = transform.position;
        gravity = Vector3.down * 9.81f;
    }

    /// <summary>
    /// Gets the maximum bounds of the camera
    /// </summary>
    public Vector2 ScreenMax
    {
        get
        {
            return new Vector2(Camera.main.transform.position.x + Camera.main.orthographicSize * Camera.main.aspect,
            Camera.main.transform.position.x + Camera.main.orthographicSize);
        }
    }

    /// <summary>
    /// Gets the minimum bounds of the camera
    /// </summary>
    public Vector3 ScreenMin
    {
        get
        {
            return new Vector2(Camera.main.transform.position.x - Camera.main.orthographicSize * Camera.main.aspect,
            Camera.main.transform.position.x - Camera.main.orthographicSize);
        }
    }

    // Update is called once per frame
    protected virtual void Update()
    {
        if (useFriction)
        {
            ApplyFriction();
        }
        if (useGravity)
        {
            ApplyGravity();
        }
        velocity += acceleration * Time.deltaTime;
        // Prevents velocity from exceeding max speed
        velocity = Vector3.ClampMagnitude(velocity, maxSpeed);

        // After velocity is calculated, before applied to position
        if (useBounce)
        {
            Bounce();
        }
        position += velocity * Time.deltaTime;
        transform.position = position;
        direction = velocity.normalized;
        acceleration = Vector3.zero;
    }

    /// <summary>
    /// Applies a given force to the object's acceleration
    /// </summary>
    /// <param name="force">Force to apply</param>
    public virtual void ApplyForce(Vector3 force)
    {
        acceleration += force / mass;
    }

    /// <summary>
    /// Applies friction to object
    /// </summary>
    protected virtual void ApplyFriction()
    {
        Vector3 friction = velocity * -1;
        friction.Normalize();
        friction *= coeff;
        ApplyForce(friction);
    }

    /// <summary>
    /// Applies gravity to the acceleration
    /// </summary>
    protected virtual void ApplyGravity()
    {
        acceleration += gravity;
    }

    /// <summary>
    /// Detects if object has gone beyond bounds of the screen and gets it to go back towards screen
    /// </summary>
    protected virtual void Bounce()
    {
        if (position.x <= ScreenMin.x)
        {
            velocity.x *= -1;
            position.x = ScreenMin.x;
        }
        else if (position.x >= ScreenMax.x)
        {
            velocity.x *= -1;
            position.x = ScreenMax.x;
        }

        if (position.y <= ScreenMin.y)
        {
            velocity.y *= -1;
            position.y = ScreenMin.y;
        }
        else if (position.y >= ScreenMax.y)
        {
            velocity.y *= -1;
            position.y = ScreenMax.y;
        }
    }

    /// <summary>
    /// Sets the position to the new specified vector
    /// </summary>
    /// <param name="newPos">Position to move object to</param>
    public virtual void SetPosition(Vector3 newPos)
    {
        position = newPos;
        transform.position = position;
    }

    protected virtual void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, new Vector2(transform.position.x + radius, transform.position.y));
    }
}
