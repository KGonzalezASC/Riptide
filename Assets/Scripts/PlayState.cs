using System;
using System.Collections;
using System.Collections.Generic;
using TMPro.Examples;
using UnityEngine;
using UnityEngine.UI;

//all gstates are monobehaviours
//this is the state that loads the game

public class PlayState : gState
{
    [SerializeField] private GameObject roadSlice; //atleast one road slice need to pass player for gameplay


    public override void Enter(gState from)
    {
        Debug.Log("Entering Game State");
        Instantiate(roadSlice, new Vector3(0, 0, 0), Quaternion.identity);
    }

    public override void Execute()
    {
        //works 
        //throw new System.NotImplementedException();
    }

    public override void Exit(gState to)
    {
        throw new System.NotImplementedException();
    }

    public override string GetName()
    {
        return "Game";
    }
}