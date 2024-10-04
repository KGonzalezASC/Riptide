using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

//all gstates are monobehaviours
//this is the state that loads the game

public class PlayState : gState
{
    [SerializeField] MainMenuEvents uiGameObject; //its fine to have direct reference cause combo text later on
    //public static Action onLost;

    public override void Enter(gState from)
    {
        Debug.Log("Entering Game State");
        //Instantiate(roadSlice, new Vector3(0, 0, 0), Quaternion.identity);
        PlatformManager.Instance.GetComponent<PlatformManager>().SpawnInitialPlatform();
        uiGameObject.StartGameplay();


        //onLost = () =>
        //{
        //    Debug.Log("You Lose");
        //    gm.switchState("Load");
        //    uiGameObject.GetComponent<MainMenuEvents>().OnRestart();
        //};
    }

    public override void Execute()
    {
        //works 
        //Platforms and hazards have to be moved in play state instead of update in next revision, this causes hazards to spawn in the wrong place and overstack lol
    }

    public override void Exit(gState to)
    {
        Debug.Log("Exiting Game State");
        uiGameObject.GetComponent<MainMenuEvents>().OnRestart();
    }

    public override string GetName()
    {
        return "Game";
    }
}