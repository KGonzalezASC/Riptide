using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrindTriggerScript : MonoBehaviour
{
    private GrindableObject parentScript;

    private void Awake()
    {
        parentScript = GetComponentInParent<GrindableObject>();
    }

    private void OnTriggerEnter(Collider collider)
    {
        if (collider.CompareTag("Player"))
        {
            //Debug.Log("Player hit grind trigger");
            parentScript.startPlayerGrinding(collider.GetComponent<FishMovement>());
        }

        else if (collider.CompareTag("DummyGrind"))
        {
            //Debug.Log("Dummy hit grind trigger"); works
            parentScript.startPlayerGrinding(collider.GetComponent<FishMovement>());
        }
    }

    private void OnTriggerExit(Collider collider)
    {
        if (collider.CompareTag("Player"))
        {
            //Debug.Log("Player exited grind trigger");
            parentScript.stopPlayerGrinding(collider.GetComponent<FishMovement>());
        }
        else if (collider.CompareTag("DummyGrind"))
        {
            //Debug.Log("Dummy exited grind trigger");
            parentScript.stopPlayerGrinding(collider.GetComponent<FishMovement>());
        }
    }
}
