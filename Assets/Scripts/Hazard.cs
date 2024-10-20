using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hazard : FlyWeight
{
    public bool isIgnored = false;

    HazardSettings Settings => (HazardSettings)base.settings;

    void MoveInPlayState()
    {
        if (!isIgnored)
        {
            // Incorporate PlayState.speedIncrement into the movement speed
            float adjustedSpeed = Settings.speed + PlayState.speedIncrement;
            transform.Translate(-Vector3.forward * (adjustedSpeed * Time.deltaTime));
        }
    }


    void Update()
    {
        if (GameManager.instance.topState.GetName() == "Game")
        {
            MoveInPlayState();
        }
    }

    IEnumerator DespawnAfterDelay(float delay)
    {
        yield return Helpers.GetWaitForSeconds(delay);
        FlyWeightFactory.ReturnToPool(this);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Destroy")) //hits platform despawn trigger in back of game view
        {
            transform.position = new Vector3(0, -20, 0); //move to safe space
            StartCoroutine(DespawnAfterDelay(Settings.despawnDelay)); //return to pool after delay
            isIgnored = true;
        }
        if (other.CompareTag("Player"))
        {
            var Fish = other.GetComponent<FishMovement>();
            switch (this.name)
            {
                case "Hazard":
                    if (Fish.powerUpState == 0)
                    {
                        Debug.Log("Player hit by hazard");
                        SFXManager.instance.playSFXClip(SFXManager.instance.hitHazardSFX, transform, 1f);
                        transform.position = new Vector3(0, -20, -5); // Move to safe space
                        GameManager.instance.switchState("YouLose"); // Switch to lose state
                    }
                    else
                    {
                        Debug.Log("Player hit by hazard but has powerup");
                        //Insert bottle break soundFX
                        transform.position = new Vector3(0, -20, -5); // Move to safe space
                        StartCoroutine(DespawnAfterDelay(Settings.despawnDelay)); // Return to pool after delay
                    }
                    break;
                case "PowerUp":
                    Debug.Log("Player hit a powerup");
                    transform.position = new Vector3(0, -20, 0); // Move to safe space
                    Fish.StartCoroutine(Fish.PowerupTime(13));
                    StartCoroutine(DespawnAfterDelay(Settings.despawnDelay)); // Return to pool after delay
                    isIgnored = true;
                    break;
                default:
                    // Is coin 
                    transform.position = new Vector3(0, -20, 0);
                    StartCoroutine(DespawnAfterDelay(Settings.despawnDelay));
                    SFXManager.instance.playSFXClip(SFXManager.instance.collectCoinSFX, transform, .025f);
                    // Combo text and score increase
                    var playstate = (GameManager.instance.topState as PlayState);
                    playstate.showComboText();
                    playstate.IncreaseScore();
                    isIgnored = true;
                    break;
            }
        }
    }
}
