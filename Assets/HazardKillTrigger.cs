using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class HazardKillTrigger : MonoBehaviour
{
    private Hazard parentScript;

    private FishMovement player = null;

    private void Update()
    {
        if ((player == null || parentScript == null) && GameManager.instance.topState.GetName() == "Game")
        {
            if (GameObject.FindWithTag("Player") != null && player == null)
            {
                player = GameObject.FindWithTag("Player").GetComponent<FishMovement>();

                //if (player != null)
                //{
                //    UnityEngine.Debug.Log("Kill trigger found player");
                //}
                //else
                //{
                //    UnityEngine.Debug.Log("Kill trigger failed to find player");
                //}
            }

            if (parentScript == null)
            {
                parentScript = GetComponentInParent<Hazard>();

                //if (parentScript != null)
                //{
                //    UnityEngine.Debug.Log("Kill trigger found parent");
                //}
                //else
                //{
                //    UnityEngine.Debug.Log("Kill trigger failed to find parent");
                //}
            }
        }
    }


    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            //SFXManager.instance.playSFXClip(SFXManager.instance.hitHazardSFX, transform, .025f);
            parentScript.HandleHazardCollision(player);
        }
    }
}
