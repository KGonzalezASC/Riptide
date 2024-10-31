using System.Collections;
using System.Collections.Generic;
//using System.Diagnostics;
using UnityEngine;

public class HazardBounceTrigger : MonoBehaviour
{
    private Hazard parentScript;

    private FishMovement player = null;

    private void Awake()
    {
        parentScript = GetComponentInParent<Hazard>();
    }

    private void Update()
    {
        if (player == null && GameManager.instance.topState.GetName() == "Game" && GameObject.FindWithTag("Player") != null)
        {
            player = GameObject.FindWithTag("Player").GetComponent<FishMovement>();
            //Debug.Log("Bounce trigger found player");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            //Debug.Log("Player hit bounce trigger");
            player.setHazardBounceReady(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            //Debug.Log("Player left bounce trigger");
            player.setHazardBounceReady(false);
        }
    }
}
