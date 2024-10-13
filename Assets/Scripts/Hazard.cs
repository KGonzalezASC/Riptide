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
            StartCoroutine(DespawnAfterDelay(Settings.despawnDelay)); //testing to see if we can hit pool size
            isIgnored = true;

          
        }
        if (other.CompareTag("Player"))
        {
            if (this.name != "Hazard")
            { //is coin/bottle cap
                transform.position = new Vector3(0, -20, 0); //move to safe space
                StartCoroutine(DespawnAfterDelay(Settings.despawnDelay)); //testing to see if we can hit pool size
                isIgnored = true;
            }
            else
            {
                Debug.Log("Player hit by hazard");
                //fire an event
                transform.position = new Vector3(0, -20, -5); //move to safe space
                GameManager.instance.switchState("YouLose"); //change to lose state and implement lose state
            }
        }
    }
}
