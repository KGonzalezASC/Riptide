using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PerfectDismountTriggerScript : MonoBehaviour
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
            parentScript.preparePlayerPerfectDismount(other.GetComponent<FishMovement>());
        }

        //check for DummyGrind tag
        if (other.CompareTag("DummyGrind"))
        {
            parentScript.preparePlayerPerfectDismount(other.GetComponent<FishMovement>());
            //demo jump
            Debug.Log("demo jump");
            if (GameManager.instance.topState.GetName() == "Load")
                other.GetComponent<FishMovement>().DemoJump();
                other.GetComponent<FishMovement>().DemoFlip();

        }
    }
}
