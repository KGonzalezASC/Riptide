using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

//all gstates are monobehaviours wrappeers for the state machine
public class LoadState : gState
{
    [SerializeField] MainMenuEvents uiGameObject;
    //camera transition fields
    [Header("Camera Transition")]
    [SerializeField] private Transform cameraTransform;

    [SerializeField] private Vector3 endPosition = Vector3.zero;
    [SerializeField] private Vector3 endRotation = Vector3.zero;

    [SerializeField] private float transitionDuration = 0.3f;



    //might reference MainMenuEvents here
    protected int m_UsedAccessory = -1;

    //private game object for dummy bottle
    [SerializeField]
    private GameObject dummyBottle;

    private GameObject activeBottle;
    [SerializeField]
    private GameObject demoRoom;


    public override void Enter(gState from)
    {
        uiGameObject.gameObject.SetActive(true);
        uiGameObject.OnPlayButtonClicked += LoadGame;

        this.co = CameraTransition(cameraTransform, transitionDuration, endPosition, endRotation);
        StopCoroutine(from.co);
        StartCoroutine(this.co);

        //set demo room to active
        demoRoom.SetActive(true);
    }


    //Tick /update if we have transparent UI with something in background we can do that here
    public override void Execute()
    {
        // Debug.Log("Loading Game");
        //rotate a character on screen maybe id
        //we want to await the callback from MainMenuEvents OnPlayGameClick
        //rotate example routine only updates in this state


        //if dummy bottle is null instantiate it and if demo room is active
        if (activeBottle == null && demoRoom.activeSelf)
        {
            activeBottle = Instantiate(dummyBottle, new Vector3(-2.95000005f, -0.629999995f, -31.5200005f), Quaternion.identity);
        }
    }

    public override void Exit(gState to)
    {
        demoRoom.SetActive(false);
    }


    public override string GetName()
    {
        return "Load";
    }
    public void LoadGame() => gm.switchState("Game");
}
