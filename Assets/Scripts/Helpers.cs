using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;


public static class Helpers
{
    //we can resuse WaitForSeconds objects
    static readonly Dictionary<float, WaitForSeconds> waitForSeconds = new ();
    public static WaitForSeconds GetWaitForSeconds(float seconds)
    {
        if(waitForSeconds.TryGetValue(seconds, out var waitForSecond))
        {
            return waitForSecond;
        }
        var wait = new WaitForSeconds(seconds);
        waitForSeconds.Add(seconds, wait);
        return waitForSeconds[seconds];
    }
}
