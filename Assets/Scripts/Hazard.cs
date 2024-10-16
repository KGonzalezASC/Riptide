using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hazard: FlyWeight
{
    public bool isIgnored = false;

    HazardSettings Settings => (HazardSettings)base.settings;

    void MoveInPlayState()
    {
        if (!isIgnored)
            transform.Translate(-Vector3.forward * (Settings.speed * Time.deltaTime));
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
            if (this.name != "Hazard")
            {   //is coin/bottle cap
                transform.position = new Vector3(0, -20, 0); 
                StartCoroutine(DespawnAfterDelay(Settings.despawnDelay));
                SFXManager.instance.playSFXClip(SFXManager.instance.collectCoinSFX, transform, .025f);
                //combo text                 
                var playstate = (GameManager.instance.topState as PlayState);
                playstate.showComboText();
                playstate.IncreaseScore();
                isIgnored = true;
            }
            else
            {
                Debug.Log("Player hit by hazard");
                SFXManager.instance.playSFXClip(SFXManager.instance.hitHazardSFX, transform, 1f);
                transform.position = new Vector3(0, -20, -5); //move to safe space
                GameManager.instance.switchState("YouLose"); //change to lose state and implement lose state
            }
        }
    }
}
