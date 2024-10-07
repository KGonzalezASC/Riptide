using UnityEngine;

//handles applying forces on object
public class PhysicsObject : MonoBehaviour
{
    Vector3 position;
    Vector3 direction;
    Vector3 velocity;
    Vector3 acceleration;
    Vector3 gravity;
    Vector3 buoyancy;

    [SerializeField]
    float maxSpeed = 25;

    [SerializeField]
    float mass = 1;

    [SerializeField]
    float coeff;

    [SerializeField]
    bool useFriction;

    [SerializeField]
    bool useGravity;

    [SerializeField]
    bool useBuoyancy;

    [SerializeField]
    float radius;

    [SerializeField]
    bool useBounce = true;

    /// <summary>
    /// gets the velocity
    /// </summary>
    public Vector3 Velocity { get { return velocity; } }

    /// <summary>
    /// gets the maximum speed of the object
    /// </summary>
    public float MaxSpeed { get { return maxSpeed; } }

    /// <summary>
    /// gets the radius of the physics object
    /// </summary>
    public float Radius { get { return radius; } }

    // Start is called before the first frame update
    void Start()
    {
        position = transform.position;

        gravity = Vector3.down * 9.81f;

        buoyancy = Vector3.up * 20.00f;
    }

    /// <summary>
    /// gets the maximum bounds of the camera
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
    /// gets the minimum bounds of the camera
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
    void Update()
    {
        if (useFriction)
        {
            ApplyFriction();
        }
        if (useGravity)
        {
            ApplyGravity();
        }
        if (useBuoyancy)
        {
            ApplyBuoyancy();
        }
        velocity += acceleration * Time.deltaTime;
        //prevents velocity from exceeding max speed
        velocity = Vector3.ClampMagnitude(velocity, maxSpeed);

        //after velocity is calculated, before applied to pos
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
    /// applies a given force to obj's acceleration
    /// </summary>
    /// <param name="force">force to apply</param>
    public void ApplyForce(Vector3 force)
    {
        acceleration += force / mass;
    }

    /// <summary>
    /// applies friction to object
    /// </summary>
    private void ApplyFriction()
    {
        Vector3 friction = velocity * -1;
        friction.Normalize();
        friction *= coeff;
        ApplyForce(friction);
    }

    /// <summary>
    /// applies gravity to the acceleration
    /// </summary>
    private void ApplyGravity()
    {
        acceleration += gravity;
    }

    /// <summary>
    /// applies buoyancy to the acceleration
    /// </summary>
    private void ApplyBuoyancy()
    {
        acceleration -= buoyancy;
    }

    /// <summary>
    /// detects if object has gone beyond bounds of 
    /// sreeen and gets it to go back towards screen
    /// </summary>
    private void Bounce()
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
    /// sets the position to the new specified vector
    /// </summary>
    /// <param name="newPos">position to move obj to</param>
    public void SetPosition(Vector3 newPos)
    {
        position = newPos;
        transform.position = position;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, new Vector2(transform.position.x + radius, transform.position.y));
    }
}