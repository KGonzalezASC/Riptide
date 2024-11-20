using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DummyFish : MonoBehaviour
{
    [SerializeField]
    private FishMovement fishMovement;
    [SerializeField]
    private float hazardBounceDelay;


    void Start()
    {
        if (CompareTag("DummyBottle")) 
        {
            fishMovement.powerUpState = FishPowerUpState.BOTTLEBREAKER;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (GameManager.instance.topState.GetName() == "Load" && CompareTag("DummyBounce") && fishMovement != null)
        {
            fishMovement.DemoJump();
        }    
    }
    //set bounce ready
    public void setHazardBounceReady(bool ready)
    {
        fishMovement.setHazardBounceReady(ready);
    }

    public void bottleBounce()
    {
        StartCoroutine(Delay(hazardBounceDelay));
    }


    //ienumerator delay
    public IEnumerator Delay(float delay)
    {
        yield return Helpers.GetWaitForSeconds(delay);
        fishMovement.hazardBounce();
    }
}
