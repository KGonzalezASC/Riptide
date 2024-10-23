using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using Object = UnityEngine.Object;

public class YouLoseState : gState
{

    [SerializeField] private MainMenuEvents uiGameObject;

    private PlayState playState;

    public override void Enter(gState from)
    {
        //get a reference to previous game state as a play state
        playState = from as PlayState;
    }


    public override void Execute()
    {

    }

    public override void Exit(gState to)
    {
        //TODO: implement some way to type in name
        PlayerSaveData.Instance.InsertScore(playState.ScoreTracker.Score, "");
    }

    public override string GetName()
    {
        return "YouLose";
    }
}
