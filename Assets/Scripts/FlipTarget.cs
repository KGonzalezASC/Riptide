using UnityEngine;

public class FlipTarget : Hazard
{
    // Parameters for sinusoidal movement
    public float amplitude = 1f; // Height of the wave
    public float frequency = 1f; // Speed of the wave

    private float timeElapsed;

    // Override the MoveInPlayState method to move in a sinusoidal wave
    protected override void MoveInPlayState()
    {
        // Adjust movement speed by adding the speed increment
        float adjustedSpeed = Settings.speed + (PlayState.speedIncrement * 0.25f);

        // Move forward in the Z-axis
        Vector3 forwardMovement = Vector3.back * (adjustedSpeed * Time.deltaTime);

        // Calculate the vertical offset using the Cosine function
        float yOffset = amplitude * Mathf.Cos(frequency * timeElapsed);

        // Combine forward movement with the sinusoidal movement
        Vector3 sinusoidalMovement = forwardMovement + new Vector3(0, yOffset, 0);

        // Apply the movement in world space
        transform.Translate(sinusoidalMovement, Space.World);

        // Increment time for the cosine wave calculation
        timeElapsed += Time.deltaTime;
        //set roation to 90 in x 
        transform.rotation = Quaternion.Euler(90, 0, 0);
    }

    protected override void Update()
    {
        MoveInPlayState();
    }
}
