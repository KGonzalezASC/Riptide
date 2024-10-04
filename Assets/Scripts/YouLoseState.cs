using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

public class YouLoseState : gState
{

    public override void Enter(gState from)
    {
        Debug.Log("Insert lose screen");       
    }


    public override void Execute()
    {
        
    }

    public override void Exit(gState to)
    {
       
    }

    public override string GetName()
    {
        return "YouLose";
    }
}
