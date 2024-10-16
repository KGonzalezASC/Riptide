using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class ScoreTracker : MonoBehaviour
{
    private int score = 0;
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

    /// <summary>
    /// sets the value of time
    /// </summary>
    public float TimeValue { set { time = value; } }


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
            timeLabel.text = "Time: " + time;
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
}
