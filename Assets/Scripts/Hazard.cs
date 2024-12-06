using System;
using System.Collections;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.Playables;

public class Hazard : FlyWeight
{
    public bool isIgnored = false;

    protected HazardSettings Settings => (HazardSettings)base.settings;
    
    protected virtual void Update()
    {
        // Only move the hazard when in "Game" state and if it's not ignored
        if (!isIgnored && GameManager.instance.topState.GetName() == "Game")
        {
            MoveInPlayState();
        }
    }

    protected virtual void MoveInPlayState()
    {
        // Adjust movement speed by adding the speed increment
        float adjustedSpeed = Settings.speed + PlayState.speedIncrement;
        // Move in world space along the Z-axis (forward direction in world space) //allows to locally rotate any hazard
        transform.Translate(Vector3.back * (adjustedSpeed * Time.deltaTime), Space.World);
    }

    public float ReturnAdjustedSpeed()
    {
        return Settings.speed + PlayState.speedIncrement;
    }


    private IEnumerator DespawnAfterDelay(float delay)
    {
        yield return Helpers.GetWaitForSeconds(delay);
        FlyWeightFactory.ReturnToPool(this);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Destroy"))
        {
            HandleOutofBounds();
        }
        else if (other.CompareTag("Player"))
        {
            HandlePlayerCollision(other.GetComponent<FishMovement>());
        }
    }

    private void HandleOutofBounds()
    {
        MoveToSafeSpace();
        StartCoroutine(DespawnAfterDelay(Settings.despawnDelay));
        isIgnored = true;
    }

    private void HandlePlayerCollision(FishMovement fish)
    {
        switch (this.name)
        {
            case "Hazard":
                //HandleHazardCollision(fish);
                break;

            case "PowerUp":
                HandlePowerUpCollision(fish);
                break;

            case "FlipTarget":
                if (fish.GetFishState() == FishMovementState.TRICK)
                {
                    Debug.Log("FlipTarget collision");
                    SFXManager.instance.playSFXClip(SFXManager.instance.bottleBreakerSFX, transform, .35f);
                    Instantiate(Settings.impactParticle, transform.position + new Vector3(0f, 0.0f, .5f), Quaternion.identity);
                    HandleCollectibleCollision();
                    fish.hazardBounce();
                }
                else
                {
                   HandleCollectibleCollision();
                }
                break;

            default: // Assume it is a collectible like a coin
                HandleCollectibleCollision();
                break;
        }
    }

    public void HandleHazardCollision(FishMovement fish)
    {
        if (fish.powerUpState == 0)
        {
            // Player hit a hazard without a power-up
            fish.OnFishDeath();
            SFXManager.instance.playSFXClip(SFXManager.instance.hitHazardSFX, transform, 1f);
            GameManager.instance.switchState("YouLose");
        }
        else
        {
            // Player hit a hazard but has a power-up
            SFXManager.instance.playSFXClip(SFXManager.instance.bottleBreakerSFX, transform, .35f);
            Instantiate(Settings.impactParticle,transform.position + new Vector3(0f, 0.0f, .5f), Quaternion.identity);
            StartCoroutine(DespawnAfterDelay(Settings.despawnDelay));
            var playState = GameManager.instance.topState as PlayState;
            playState.IncreaseScore();
        }
        MoveToSafeSpace();
    }

    private void HandlePowerUpCollision(FishMovement fish)
    {
        StartCoroutine(DespawnAfterDelay(Settings.despawnDelay)); //we can not handle the routine for managing powerup here since they get depooled / deleted..
        isIgnored = true;
        MoveToSafeSpace();
        var playState = GameManager.instance.topState as PlayState;
        playState.PowerupTime(fish);

    }


    public float getSpeed() {
        return Settings.speed + PlayState.speedIncrement;
    }



    private void HandleCollectibleCollision()
    {
        //Debug.Log("Player collected an item");
        SFXManager.instance.playSFXClip(SFXManager.instance.collectCoinSFX, transform, .025f);
        // Update game state (combo text, score)
        var playState = GameManager.instance.topState as PlayState;
        if (GameManager.instance.topState.GetName() == "Game")
        {
            //playState.showComboText();
            playState.IncreaseScore();
        }
        isIgnored = true;
        MoveToSafeSpace();
        StartCoroutine(DespawnAfterDelay(Settings.despawnDelay));
    }

    private void MoveToSafeSpace()
    {
        transform.position = new Vector3(0, -20, 0);

        if (GetComponentInChildren<HazardBounceTrigger>() != null)
        {
            //UnityEngine.Debug.Log("Trying to reset material");
            GetComponentInChildren<HazardBounceTrigger>().ResetMat();
        }
    }
}
