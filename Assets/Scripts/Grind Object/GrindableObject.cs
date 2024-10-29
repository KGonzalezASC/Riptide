using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrindableObject : MonoBehaviour
{
    [SerializeField] public FishMovement player = null;
    private BoxCollider grindBox;
    private BoxCollider killBox;

    private void Awake()
    {

    }

    private void OnEnable()
    {
        grindBox = transform.GetChild(2).GetComponent<BoxCollider>();

        if (grindBox)
        {
            //Debug.Log("Grind object found grind box");
        }
        else
        {
            Debug.Log("Grind object couldn't find grind box");
        }

        killBox = transform.GetChild(3).GetComponent<BoxCollider>();

        if (killBox)
        {
            //Debug.Log("Grind object found kill box");
        }
        else
        {
            Debug.Log("Grind object couldn't find kill box");
        }
    }

    // Update is called once per frame
    private void FixedUpdate()
    {
        if (player == null && GameManager.instance.topState.GetName() == "Game" && GameObject.FindWithTag("Player") != null)
        {
            player = GameObject.FindWithTag("Player").GetComponent<FishMovement>();

            if (player)
            {
                //Debug.Log("Grind object found player");
            }
            else
            {
                Debug.Log("Grind object couldn't find player");
            }
        }

    }

    public void startPlayerGrinding()
    {
        //Debug.Log("Attempting grinding start");
        player.startGrind(transform.position.x, transform.position.y + 0.63f);
    }

    public void stopPlayerGrinding()
    {
        //Debug.Log("Attempting grinding stop");
        player.stopGrind();
    }

    public void preparePlayerPerfectDismount()
    {
        Debug.Log("Player ready for perfect dismount");
        player.preparePerfectDismount();
    }

    public void killPlayer()
    {
        Debug.Log("Player hit by hazard");
        player.resetState();
        GameManager.instance.switchState("Load");
    }
}
