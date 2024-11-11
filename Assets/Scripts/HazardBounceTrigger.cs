using System.Collections;
using System.Collections.Generic;
//using System.Diagnostics;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;

public class HazardBounceTrigger : MonoBehaviour
{
    private Hazard parentScript;

    private FishMovement player = null;

    private Light light;
    [SerializeField] private Material regularMaterial;
    [SerializeField] private Material highlightedMaterial;

    private void Awake()
    {
        parentScript = GetComponentInParent<Hazard>();
        light = transform.parent.GetChild(2).GetComponent<Light>();
        light.intensity = 0;
        transform.parent.GetComponent<MeshRenderer>().material = regularMaterial;
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
            light.intensity = 15;
            transform.parent.GetComponent<MeshRenderer>().material = highlightedMaterial;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            //Debug.Log("Player left bounce trigger");
            player.setHazardBounceReady(false);
            ResetMat();
        }
    }

    public void ResetMat()
    {
        transform.parent.GetComponent<MeshRenderer>().material = regularMaterial;
        light.intensity = 0;
    }
}
