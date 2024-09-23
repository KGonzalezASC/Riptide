using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

//all gstates are monobehaviours
//this is the state that loads the game

public class LoadState : gState
{
    //might reference MainMenuEvents here
    protected int m_UsedAccessory = -1;

    public override void Enter(gState from)
    {
        Debug.Log("Entering Load State");
        // u can start any coroutine here if needed
        //start music here if needed
        //setup ui stuff

    }

    //Tick /update

    public override void Execute()
    {
        // Debug.Log("Loading Game");
        //check if ui stuff is pressed
        //rotate a character on screen maybe idk
    }

    public override void Exit(gState to)
    {
        Debug.Log("Exiting Load State");
    }

    public override string GetName()
    {
        return "Load";
    }
}
