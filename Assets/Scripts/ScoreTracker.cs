using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;

//using System.Diagnostics;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.UIElements;

public class ScoreTracker : MonoBehaviour
{
    private int score = 0;
    private int scoreSum = 0;
    private float scoreMult = 1.0f;
    private UIDocument _document;

    public UIDocument Document
    {
        get { return _document; }
        set { _document = value; }
    }
    public Label ScoreLabel { set { scoreLabel = value; } }
    public Label TimeLabel { set { timeLabel = value; } }

    private Label scoreLabel;
    private Label timeLabel;

    [SerializeField]
    private GameObject pipe;
    [SerializeField]
    private Light directionalLight;

    [SerializeField]
    private DisplayComboText textDisplay;


    /// <summary>
    /// gets/sets the value od score
    /// </summary>
    public int Score
    {
        get { return score; }
        set { score = value; }
    }

    //value to scale based on time survived
    const float timeScale = 0.001f;

    private float time = 0;

    public float TimeValue
    {
        get { return time; }
        set { time = value; }
    }


    // Start is called before the first frame update
    void Start()
    {
        //ensure values are reset
        score = 0;
        time = 0;
        GenerateGradientColors();
    }

    private void Update()
    {
        time += Time.deltaTime;
        if (GameManager.instance.topState.GetName() == "Game")
        {
            scoreLabel.text = "Score: " + score;
            timeLabel.text = "Time: " + (int)time;
        }
    }


    /// <param name="baseAmount">base value added to the player's score</param>
    private int lastIntensityDecreaseThreshold = 0;

    public void IncrementScore(int baseAmount)
    {
        // Increment the score based on the provided formula
        score += (int)(baseAmount + baseAmount * (time * timeScale));

        // Calculate the next threshold for color change
        int nextThreshold = ((score / 3000) + 1) * 3000;

        // If the score has crossed a 3000-point boundary
        if (score >= nextThreshold - 3000)
        {
            // Get the pipe's material
            Material pipeMaterial = pipe.GetComponent<Renderer>().material;

            // Calculate the index in the colors array
            int colorIndex = (score / 3000) % colors.Length;

            // Set the pipe's color to the new color
            pipeMaterial.color = colors[colorIndex];
        }

        // Decrement the light intensity every 6000 points
        int intensityDecreaseThreshold = (score / 3000) * 3000;

        if (intensityDecreaseThreshold > lastIntensityDecreaseThreshold)
        {
            if (directionalLight.intensity > 0.03f)
            {
                directionalLight.intensity -= 0.13f;
                lastIntensityDecreaseThreshold = intensityDecreaseThreshold; // Update the threshold
            }
        }
    }



    /// <summary>
    /// Adds to a score sum, which will be added to total score when gainTrickScore is called
    /// </summary>
    /// <param name="addedAmount">the amount to add to the score sum</param>
    public void buildTrickScore(int addedAmount)
    {
        scoreSum += addedAmount;
        //Debug.Log("scoreSum: " + scoreSum);

        //quick and dirty fix so that grinding on a rail doesnt overload screen
        //also prevents it from showing up when dismounting
        if (addedAmount > 5)
        {
            textDisplay.ChangeText();
        }
    }

    /// <summary>
    /// Adds to a score multiplier, to be applied when the trick score is added to the total
    /// </summary>
    /// <param name="addedMult">the amount to increase the multiplier by</param>
    public void buildTrickMultiplier(float addedMult)
    {
        if (scoreSum > 0)
        {
            scoreMult += addedMult;
            //Debug.Log("scoreMult: " + scoreMult);
        }
    }

    /// <summary>
    /// Adds trick score to the total
    /// </summary>
    /// <param name="perfect">score multipier is multiplied by 1.5x when true</param>
    public void gainTrickScore(bool perfect)
    {
        if (scoreSum > 0)
        {
            if (perfect)
            {
                scoreMult *= 1.5f;
            }

            scoreSum = (int)(scoreSum * scoreMult);

            IncrementScore(scoreSum);
            //Debug.Log("Added " + scoreSum + " * " + scoreMult + " = " + (scoreSum * scoreMult) + " to score ");

            scoreSum = 0;
            scoreMult = 1.0f;
        }

        //combo is finished, clear combo text
        textDisplay.ClearText();
    }

    /// <summary>
    /// Resets trick score sum if the player messes up
    /// </summary>
    public void loseTrickScore()
    {
        scoreSum = 0;
        scoreMult = 1.0f;
    }


    private Color[] colors;

    private void GenerateGradientColors()
    {
        int stepsPerSegment = 3;
        colors = new Color[40];

        // Define key colors for gradient
        Color green = new (0f / 255f, 255f / 255f, 0f / 255f);  // Green
        Color blue = new (0f / 255f, 0f / 255f, 255f / 255f);   // Blue
        Color yellow = new (255f / 255f, 255f / 255f, 0f / 255f); // Yellow
        Color red = new (255f / 255f, 0f / 255f, 0f / 255f);    // Red
        Color black = new (0f / 255f, 0f / 255f, 0f / 255f);    // Black

        // Gradually interpolate between each color pair
        int index = 0;
        for (int i = 0; i < stepsPerSegment; i++) colors[index++] = Color.Lerp(green, blue, (float)i / stepsPerSegment);
        for (int i = 0; i < stepsPerSegment; i++) colors[index++] = Color.Lerp(blue, yellow, (float)i / stepsPerSegment);
        for (int i = 0; i < stepsPerSegment; i++) colors[index++] = Color.Lerp(yellow, red, (float)i / stepsPerSegment);
        for (int i = 0; i < stepsPerSegment; i++) colors[index++] = Color.Lerp(red, black, (float)i / stepsPerSegment);
    }


    //set reset color method to first color in array
    public void resetColor()
    {
        Material pipeMaterial = pipe.GetComponent<Renderer>().material;
        pipeMaterial.color = colors[0];
        //reset light intensity
        directionalLight.intensity = 2.4f;
        lastIntensityDecreaseThreshold = 0;
    }
}
