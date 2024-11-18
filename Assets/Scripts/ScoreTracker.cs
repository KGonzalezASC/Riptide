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

        //other fields are set in the game manager's game state OnPlay()
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
    public void IncrementScore(int baseAmount)
    {
        // Increment the score based on the provided formula
        score += (int)(baseAmount + baseAmount * (time * timeScale));

        // Calculate the next threshold the score should reach for color change
        int nextThreshold = ((score / 5000) + 1) * 5000;

        // If the score has crossed a 5000-point boundary
        if (score >= nextThreshold - 5000)
        {
            // Get the pipe's material
            Material pipeMaterial = pipe.GetComponent<Renderer>().material;

            // Calculate the index in the colors array
            int colorIndex = (score / 5000) % colors.Length;

            // Set the pipe's color to the new color
            pipeMaterial.color = colors[colorIndex];


            //decerment light intensity up to .03f by substracting .1f every 5000 points
            if (directionalLight.intensity > .03f && score % 5000==0)
            {
                directionalLight.intensity -= .03f;
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
        textDisplay.ChangeText();
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
    }

    /// <summary>
    /// Resets trick score sum if the player messes up
    /// </summary>
    public void loseTrickScore()
    {
        scoreSum = 0;
        scoreMult = 1.0f;
    }


    //array of colors 
    public Color[] colors = new Color[]
   {
        new(255f / 255f, 0f / 255f, 204f / 255f), // FF00CC
        new(125f / 255f, 70f / 255f, 0f / 255f),   // 7D4600
        new(245f / 255f, 143f / 255f, 41f / 255f),  // F58F29
        new(164f / 255f, 176f / 255f, 245f / 255f),// A4B0F5
        new(68f / 255f, 100f / 255f, 173f / 255f)   // 4464AD
   };

    //set reset color method to first color in array
    public void resetColor()
    {
        Material pipeMaterial = pipe.GetComponent<Renderer>().material;
        pipeMaterial.color = colors[0];
        //reset light intensity
        directionalLight.intensity = .79f;
    }
}
