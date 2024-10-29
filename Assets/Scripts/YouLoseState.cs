using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

public class YouLoseState : gState
{
    [SerializeField] private UIDocument loseUIDocument; //lose screen ui document
    private VisualElement scoreContainer;
    private Label _label;
    private PlayState playState;

    public override void Enter(gState from)
    {
        //get a reference to previous game state as a play state
        playState = from as PlayState;
        // Initialize score container
        scoreContainer = loseUIDocument.rootVisualElement.Q<VisualElement>("container-score"); // Replace with actual container ID
        PopulateScores();
    }


    public override void Execute()
    {

    }

    /// <summary>
    /// Populates the score container on lose UI
    /// </summary>
    private void PopulateScores()
    {
        //clear existing scores
        scoreContainer.Clear();
        PlayerSaveData.Instance.InsertScore(playState.ScoreTracker.Score, "Player"); // TODO: add name input
        foreach (HighscoreEntry entry in PlayerSaveData.Instance.highscores)
        {
            //create label
            Label scoreLabel = new Label($"{entry.name}: {entry.score}");
            //add styling
            scoreLabel.AddToClassList("label-score");
            //add to container
            scoreContainer.Add(scoreLabel);
        }
    }

    public override void Exit(gState to)
    {
        //TODO: implement some way to type in name
        PlayerSaveData.Instance.Save();
    }

    public override string GetName()
    {
        return "YouLose";
    }
}
