using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

//all gstates are monobehaviours wrappeers for the state machine
public class PlayState : gState
{
    [SerializeField] MainMenuEvents uiGameObject; 
    private DisplayComboText comboText;
    private ScoreTracker scoreTracker;

    /// read only public reference to score tracker
    public ScoreTracker ScoreTracker { get { return scoreTracker; } }

    //static float to increment speed of the game
    public static float speedIncrement = 0.01f;
    private Coroutine difficultyCoroutine;
    private Coroutine powerUpCoroutine = null;

    private const int scoreValue = 100;

    [SerializeField] Transform cameraTransform;
    [SerializeField] float transitionDuration;
    [SerializeField] Vector3 gameplayCamPos = Vector3.zero;
    [SerializeField] Vector3 gameplayCamRotation = Vector3.zero;

    [SerializeField] GameObject waterMaterial;
    private float powerUpTimer = 0f;


    public void Awake() //gstates are monobehaviours so they can an have awake
    {
        comboText = uiGameObject.GetComponent<DisplayComboText>();
        scoreTracker = uiGameObject.GetComponent<ScoreTracker>();
        scoreTracker.Document = uiGameObject.GetComponent<UIDocument>();
    }

    public void showComboText()
    {
        comboText.ChangeText();
    }

    public void IncreaseScore()
    {
        scoreTracker.IncrementScore(scoreValue);
    }

    public void PowerupTime(FishMovement player)
    {
        IncreaseScore();
        //increase powerup time if powerup is already active
        powerUpTimer += 5.6f;
        if (powerUpCoroutine == null)
        {
            //set initial powerup time = 20 seconds
            powerUpTimer = 20f;
            powerUpCoroutine = StartCoroutine(PowerUpTimer(player));
        }

    }

    public void ExtendTimer() => powerUpTimer +=.45f;

    private IEnumerator PowerUpTimer(FishMovement player)
    {
        player.powerUpState = FishPowerUpState.BOTTLEBREAKER;
        while (powerUpTimer > 0f)
        {
            powerUpTimer -= Time.deltaTime;
            //pass reference to powerup timer since we need to change the value of the progress bar and a copy of the value would not work
            comboText.powerUpTimerLength(ref powerUpTimer);
            if (powerUpTimer < 2f) {
                //if close to 2 seconds left, invoke player raycast method to check for hazards
                player.CheckForHazards();
            }
            yield return null;
        }
        player.powerUpState = FishPowerUpState.NONE;
        powerUpCoroutine = null;
    }



    public override void Enter(gState from) //since the ui document changes we need to reassign the document labels and score
    {
        Debug.Log("Entering Game State");
        PlatformManager.Instance.GetComponent<PlatformManager>().SpawnInitialPlatform();
        if (GameObject.Find("FishBoard(Clone)") == null)
        {
            ConfigObject.Instance.InstantiatePrefab(new Vector3(0, 0, 0));
        }
        else
        {
            ConfigObject.Instance.GetInstanceRef().transform.position = new Vector3(0, 0, 0); //causes weird snapping but we can hid player in future and avoid this entirely.
        }
        uiGameObject.StartGameplay();
        scoreTracker.Score = 0;
        //reset time
        scoreTracker.TimeValue = 0;
        //assign score label
        scoreTracker.ScoreLabel = uiGameObject.GetComponent<UIDocument>().rootVisualElement.Q<Label>("label-score");
        scoreTracker.TimeLabel = uiGameObject.GetComponent<UIDocument>().rootVisualElement.Q<Label>("label-time");
        scoreTracker.resetColor();

        speedIncrement = 0.01f; //reset speed increment
        difficultyCoroutine = StartCoroutine(DifficultyHandler(6f));

        //move camera after setup
        StartCoroutine(CameraTransition(cameraTransform, transitionDuration, gameplayCamPos, gameplayCamRotation));
    }
    public override void Execute()
    {
        //objects can be refered to in this state through execute. OR you can do an if check in an objects update to check the current game state which is a bit more efficient
        //in terms of performance because of no for each loop
        //both approaches are valid tho

        //check if 60 seconds have passed in play state


    }
    public override void Exit(gState to)
    {
        Debug.Log("Exiting Game State");
        uiGameObject.OnRestart();
        PlatformManager.Instance.ClearAllPlatforms();
        Time.timeScale = 0;
        FlyWeightFactory.ClearPool(FlyWeightType.Coin);
        FlyWeightFactory.ClearPool(FlyWeightType.Hazard);
        FlyWeightFactory.ClearPool(FlyWeightType.GrindablePole); //the idea for the pole is that we can make specicfic script that just switches between prefab on its own settings for different types of poles
        FlyWeightFactory.ClearPool(FlyWeightType.SlopedGrindablePole);
        FlyWeightFactory.ClearPool(FlyWeightType.PowerUp);
        StopCoroutine(difficultyCoroutine);
        Time.timeScale = 1;
    }
    public override string GetName()
    {
        return "Game";
    }

    IEnumerator DifficultyHandler(float delay)
    {
        yield return Helpers.GetWaitForSeconds(0.65f);

        while (GameManager.instance.topState.GetName() == "Game")
        {
            yield return Helpers.GetWaitForSeconds(delay);

            // Increase speed increment, but cap it at a maximum of 7? playtest ig idk
            if (speedIncrement < 14f)
            {
                speedIncrement += 0.75f;

                // Ensure the speedIncrement doesn't exceed 5
                if (speedIncrement > 14f)
                {
                    speedIncrement = 14f;
                }

                Debug.Log("Game speed increased by: " + speedIncrement);
            }
        }
        Debug.Log("DifficultyHandler coroutine stopped because the game state changed.");
    }
}