using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

//all gstates are monobehaviours wrappeers for the state machine
public class LoadState : gState
{
    [SerializeField] MainMenuEvents uiGameObject;
    [SerializeField] private GameObject exampleRoutine;

    //camera transition fields
    [Header("Camera Transition")]
    [SerializeField] private Transform cameraTransform;

    [SerializeField] private Vector3 endPosition = Vector3.zero;
    [SerializeField] private Vector3 endRotation = Vector3.zero;

    [SerializeField] private float transitionDuration = 0.3f;

    //might reference MainMenuEvents here
    protected int m_UsedAccessory = -1;


    public override void Enter(gState from)
    {
        uiGameObject.gameObject.SetActive(true);
        uiGameObject.OnPlayButtonClicked += LoadGame;

        StartCoroutine(CameraTransition(cameraTransform, transitionDuration, endPosition, endRotation));
    }


    //Tick /update if we have transparent UI with something in background we can do that here
    public override void Execute()
    {
        // Debug.Log("Loading Game");
        //rotate a character on screen maybe id
        //we want to await the callback from MainMenuEvents OnPlayGameClick
        //rotate example routine only updates in this state
        exampleRoutine.transform.Rotate(0, 0, 1);
    }

    public override void Exit(gState to)
    {
        Debug.Log("Exiting Load State");
    }

    public override string GetName()
    {
        return "Load";
    }
    public void LoadGame() => gm.switchState("Game");
}
