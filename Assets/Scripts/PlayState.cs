using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

//all gstates are monobehaviours wrappeers for the state machine
public class PlayState : gState
{
    [SerializeField] MainMenuEvents uiGameObject; //its fine to have direct reference cause combo text later on
    private DisplayComboText comboText;
    private ScoreTracker scoreTracker;

    [SerializeField, Range(0f, 200f)]
    private const int scoreValue = 100;


    public void Awake() //gstates are monobehaviours so they can an have awake
    {
        comboText = uiGameObject.GetComponent<DisplayComboText>();
        scoreTracker = uiGameObject.GetComponent<ScoreTracker>();
        scoreTracker.Document = uiGameObject.GetComponent<UIDocument>();
    }

    public void showComboText() {
        comboText.ChangeText();
    }

    public void IncreaseScore()
    {
        scoreTracker.IncrementScore(scoreValue);
    }

    public override void Enter(gState from) //since the ui document changes we need to reassign the document labels and score
    {
        Debug.Log("Entering Game State");
        PlatformManager.Instance.GetComponent<PlatformManager>().SpawnInitialPlatform();
        if(GameObject.Find("FishBoard(Clone)") == null)
        {
           ConfigObject.Instance.InstantiatePrefab(new Vector3(0, 0, 0));
        }
        else
        {
            ConfigObject.Instance.GetInstanceRef().transform.position = new Vector3(0, 0, 0); //causes weird snapping but we can hid player in future and avoid this entirely.
        }
        uiGameObject.StartGameplay();
        scoreTracker.Score = 0;
        //assign score label
        scoreTracker.ScoreLabel = uiGameObject.GetComponent<UIDocument>().rootVisualElement.Q<Label>("label-score");
        scoreTracker.TimeLabel = uiGameObject.GetComponent<UIDocument>().rootVisualElement.Q<Label>("label-time");

    }
    public override void Execute()
    {
        //objects can be refered to in this state through execute. OR you can do an if check in an objects update to check the current game state which is a bit more efficient
        //in terms of performance because of no for each loop
        //both approaches are valid tho
    }
    public override void Exit(gState to)
    {
        Debug.Log("Exiting Game State");
        uiGameObject.OnRestart();
        Time.timeScale = 0;
        PlatformManager.Instance.ClearAllPlatforms();
        FlyWeightFactory.ClearPool(FlyWeightType.Ice);
        FlyWeightFactory.ClearPool(FlyWeightType.Fire); //TODO: rename types to match items in the game
        Time.timeScale = 1;
    }
    public override string GetName()
    {
        return "Game";
    }
}