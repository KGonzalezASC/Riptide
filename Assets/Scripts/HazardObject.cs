using UnityEngine;

public class HazardObject : PhysicsObject
{
    [SerializeField]
    float floorYLevel = 0;

    [SerializeField]
    float buoyancy = 0;

    // Start is called before the first frame update
    protected override void Start()
    {
        base.Start();
    }

    protected override void Bounce()
    {
        if (position.y <= floorYLevel)
        {
            velocity += new Vector3(0, buoyancy, 0);
        }
    }

    protected override void Update()
    {
        base.Update();
    }
}
