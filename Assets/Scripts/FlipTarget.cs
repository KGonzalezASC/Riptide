using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlipTarget : Hazard
{
    //override the MoveInPlayState method to flip the target
    protected override void MoveInPlayState()
    {
        // Adjust movement speed by adding the speed increment
        float adjustedSpeed = Settings.speed + PlayState.speedIncrement;
        // Move in world space along the Z-axis (forward direction in world space) //and angle the movement upward as they move
        transform.Rotate(Vector3.up * (adjustedSpeed * Time.deltaTime), Space.World);
        transform.Translate(Vector3.forward * (adjustedSpeed * Time.deltaTime), Space.World);
    }

    protected override void Update()
    {
       MoveInPlayState();
    }
}
