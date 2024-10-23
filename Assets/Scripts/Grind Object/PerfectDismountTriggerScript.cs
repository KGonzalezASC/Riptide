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
            parentScript.preparePlayerPerfectDismount();
        }
    }
}
