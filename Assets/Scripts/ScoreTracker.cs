using System.Collections;
using System.Collections.Generic;
//using System.Diagnostics;
using UnityEngine;
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

    private VisualTreeAsset gameplayUXML;

    private Label scoreLabel;
    private Label timeLabel;

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

    /// <summary>
    /// increases the player's score by a set amount plus an additional amount for the time survived
    /// </summary>
    /// <param name="baseAmount">base value added to the player's score</param>
    public void IncrementScore(int baseAmount)
    {
        score += (int)(baseAmount + baseAmount * (time * timeScale));
    }

    /// <summary>
    /// Adds to a score sum, which will be added to total score when gainTrickScore is called
    /// </summary>
    /// <param name="addedAmount">the amount to add to the score sum</param>
    public void buildTrickScore(int addedAmount)
    {
        scoreSum += addedAmount;
        Debug.Log("scoreSum: " + scoreSum);
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
            Debug.Log("scoreMult: " + scoreMult);
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
            Debug.Log("Added " + scoreSum + " * " + scoreMult + " = " + (scoreSum * scoreMult) + " to score ");

            scoreSum = 0;
            scoreMult = 0;
        }
    }

    /// <summary>
    /// Resets trick score sum if the player messes up
    /// </summary>
    public void loseTrickScore()
    {
        scoreSum = 0;
    }
}
