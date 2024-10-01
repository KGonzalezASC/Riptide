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

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider collider)
    {
        if (collider == parentScript.player.GetComponent<SphereCollider>())
        {
            parentScript.startPlayerGrinding();
        }
    }

    private void OnTriggerExit(Collider collider)
    {
        if (collider == parentScript.player.GetComponent<SphereCollider>())
        {
            parentScript.stopPlayerGrinding();
        }
    }
}
