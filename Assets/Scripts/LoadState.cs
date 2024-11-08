using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

//all gstates are monobehaviours wrappeers for the state machine
public class LoadState : gState
{
    [SerializeField] MainMenuEvents uiGameObject;
    [SerializeField] private GameObject exampleRoutine;
    //might reference MainMenuEvents here
    protected int m_UsedAccessory = -1;

    [SerializeField] Transform cameraTransform;

    //position of camera during gameplay
    //camera in editor is already in gameplay position on game start so these are assigned on start
    [SerializeField] Vector3 loadPos = Vector3.zero;
    [SerializeField] Vector3 loadRotation = Vector3.zero;
    [SerializeField] private float transitionDuration = 1.5f; // Time for transition


    public override void Enter(gState from)
    {
        uiGameObject.gameObject.SetActive(true);
        uiGameObject.OnPlayButtonClicked += LoadGame;

        // start the camera transition from game position to menu position
        StartCoroutine(CameraTransition(cameraTransform, transitionDuration, loadPos, loadRotation));
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
