using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hazard: FlyWeight
{
    public bool isIgnored = false;

    new HazardSettings settings => (HazardSettings) base.settings;

    void OnEnable()
    {
        //StartCoroutine(DespawnAfterDelay(settings.despawnDelay));
    }

    void Update()
    {
        if(!isIgnored)
         transform.Translate(-Vector3.forward * (settings.speed * Time.deltaTime));
    }

    IEnumerator DespawnAfterDelay(float delay)
    {
        yield return Helpers.GetWaitForSeconds(delay);
        FlyWeightFactory.ReturnToPool(this); //return to pool instead of destroying

    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Destroy"))
        {
            Debug.Log("Hazard returned to the pool");
            transform.position = new Vector3(0, -20, 0); //move to safe space
            StartCoroutine(DespawnAfterDelay(settings.despawnDelay)); //testing to see if we can hit pool size
            isIgnored = true;
        }
        if (other.CompareTag("Player"))
        {
            if (this.name != "Hazard") {
                Debug.Log("Coin Collected by player, returning to the pool");
                transform.position = new Vector3(0, -20, 0); //move to safe space
                StartCoroutine(DespawnAfterDelay(settings.despawnDelay)); //testing to see if we can hit pool size
                isIgnored = true;
            }
            else
            {
                Debug.Log("Player hit by hazard");
                //fire an event
                transform.position = new Vector3(0, -20, -5); //move to safe space
                //PlayState.onLost?.Invoke();
                GameManager.instance.switchState("Load");
            }
        }

    }
}
