using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

public class YouLoseState : gState
{
    [SerializeField] private UIDocument loseUIDocument; //lose screen ui document
    private VisualElement scoreContainer;
    private Label _label;
    private PlayState playState;

    [SerializeField] Transform cameraTransform;

    //position of camera during gameplay
    //camera in editor is already in gameplay position on game start so these are assigned on start
    [SerializeField] Vector3 loadPos = Vector3.zero;
    [SerializeField] Vector3 loadRotation = Vector3.zero;
    [SerializeField] private float transitionDuration = 1.5f; //time for transition

    public override void Enter(gState from)
    {
        //get a reference to previous game state as a play state
        playState = from as PlayState;
        // Initialize score container
        scoreContainer = loseUIDocument.rootVisualElement.Q<VisualElement>("container-score"); // Replace with actual container ID
        PopulateScores();


        if (from.co != null)
            StopCoroutine(from.co);
        //set this camera coroutine
        this.co = CameraTransition(cameraTransform, transitionDuration, loadPos, loadRotation);

        //move camera after setup
        StartCoroutine(this.co);
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
        //transition to load camera position
        StartCoroutine(CameraTransition(cameraTransform, transitionDuration, loadPos, loadRotation));
    }

    public override string GetName()
    {
        return "YouLose";
    }
}
