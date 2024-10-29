using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Playables;

public class Hazard : FlyWeight
{
    public bool isIgnored = false;

    HazardSettings Settings => (HazardSettings)base.settings;

    private void Update()
    {
        // Only move the hazard when in "Game" state and if it's not ignored
        if (!isIgnored && GameManager.instance.topState.GetName() == "Game")
        {
            MoveInPlayState();
        }
    }

    private void MoveInPlayState()
    {
        // Adjust movement speed by adding the speed increment
        float adjustedSpeed = Settings.speed + PlayState.speedIncrement;
        // Move in world space along the Z-axis (forward direction in world space) //allows to locally rotate any hazard
        transform.Translate(Vector3.back * (adjustedSpeed * Time.deltaTime), Space.World);
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
                HandleHazardCollision(fish);
                break;

            case "PowerUp":
                HandlePowerUpCollision(fish);
                break;

            default: // Assume it is a collectible like a coin
                HandleCollectibleCollision();
                break;
        }
    }

    private void HandleHazardCollision(FishMovement fish)
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
            // Insert bottle break soundFX here
            StartCoroutine(DespawnAfterDelay(Settings.despawnDelay));
        }
        MoveToSafeSpace();
    }

    private void HandlePowerUpCollision(FishMovement fish)
    {
        //Debug.Log("Player hit a power-up");
        fish.StartCoroutine(fish.PowerupTime(13)); // Start power-up effect
        StartCoroutine(DespawnAfterDelay(Settings.despawnDelay));
        isIgnored = true;
        MoveToSafeSpace();
        var playState = GameManager.instance.topState as PlayState;
        playState.StartPowerSlider();
    }

    private void HandleCollectibleCollision()
    {
        //Debug.Log("Player collected an item");
        SFXManager.instance.playSFXClip(SFXManager.instance.collectCoinSFX, transform, .025f);
        // Update game state (combo text, score)
        var playState = GameManager.instance.topState as PlayState;
        if(GameManager.instance.topState.GetName() == "Game")
        playState.showComboText();
        playState.IncreaseScore();
        isIgnored = true;
        MoveToSafeSpace();
        StartCoroutine(DespawnAfterDelay(Settings.despawnDelay));
    }

    private void MoveToSafeSpace() => transform.position = new Vector3(0, -20, 0);
}
