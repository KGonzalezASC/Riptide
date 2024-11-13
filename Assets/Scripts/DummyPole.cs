using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DummyPole : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (GameManager.instance.topState.GetName() == "Load")
        {
            if(transform.position.z > -10f)
            {
                transform.position = new Vector3(transform.position.x, transform.position.y, -39f);
            }
            MoveInLoadState();
        }
    }

    private void MoveInLoadState()
    {        
        // Move in world space along the Z-axis (forward direction in world space) //allows to locally rotate any hazard
        if(transform.position.z < -10f)
            transform.Translate(-Vector3.back * (7 * Time.deltaTime), Space.World);
    }

}
