using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using UnityEngine;

public class GrindableObject : MonoBehaviour
{
    public FishMovement player = null;
    private BoxCollider grindBox;
    private BoxCollider killBox;

    [SerializeField] private float grindStartHeight = 0.0f;
    [SerializeField] private bool sloped;

    // These are both multiplied by the grind rail's speed
    [SerializeField] private float grindDirX;
    [SerializeField] private float grindDirY;

    private Vector3 playerGrindDir;

    private void OnEnable()
    {
        playerGrindDir = new Vector3(grindDirX, grindDirY, 0);

        if (grindStartHeight != -1.0f)
        {
            grindStartHeight = transform.position.y + 0.63f;
            //UnityEngine.Debug.Log("grind start height: " + grindStartHeight);
        }

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

            if (grindStartHeight != -1.0f)
            {
                grindStartHeight = transform.position.y + 0.63f;
                //UnityEngine.Debug.Log("grind start height: " + grindStartHeight);
            }
        }

        if (GameManager.instance.topState.GetName() == "Game" && transform.GetComponent<Hazard>() != null)
        {
            playerGrindDir.x = transform.GetComponent<Hazard>().ReturnAdjustedSpeed() * grindDirX;
            playerGrindDir.y = transform.GetComponent<Hazard>().ReturnAdjustedSpeed() * grindDirY;
        }
    }

    public void startPlayerGrinding()
    {
        //Debug.Log("Attempting grinding start");
        player.startGrind(transform.position.x, grindStartHeight, playerGrindDir);
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
        player.OnFishDeath();
        GameManager.instance.switchState("YouLose");
    }
}
