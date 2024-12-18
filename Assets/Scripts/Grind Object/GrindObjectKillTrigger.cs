using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrindObjectKillTrigger : MonoBehaviour
{
    private GrindableObject parentScript;

    private void Awake()
    {
        parentScript = GetComponentInParent<GrindableObject>();
    }


    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            var fish = other.GetComponent<FishMovement>();
            SFXManager.instance.playSFXClip(SFXManager.instance.hitHazardSFX, transform, .025f);
            fish.OnFishDeath();
            GameManager.instance.switchState("YouLose");
        }
    }
}
