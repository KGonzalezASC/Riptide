using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrindableObject : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] public FishMovement player = null;
    private BoxCollider grindBox;
    private BoxCollider killBox;

    private void Awake()
    {

    }

    private void OnEnable()
    {
        //player = GameObject.FindWithTag("Player").GetComponent<FishMovement>();

        //if (player)
        //{
        //    Debug.Log("Grind object found player");
        //}
        //else
        //{
        //    Debug.Log("Grind object couldn't find player");
        //}

        grindBox = transform.GetChild(2).GetComponent<BoxCollider>();

        if (grindBox)
        {
            Debug.Log("Grind object found grind box");
        }
        else
        {
            Debug.Log("Grind object couldn't find grind box");
        }

        killBox = transform.GetChild(3).GetComponent<BoxCollider>();

        if (killBox)
        {
            Debug.Log("Grind object found kill box");
        }
        else
        {
            Debug.Log("Grind object couldn't find kill box");
        }
    }

    // Update is called once per frame
    private void FixedUpdate()
    {
        if (player == null && GameManager.instance.topState.GetName() == "Game")
        {
            player = GameObject.FindWithTag("Player").GetComponent<FishMovement>();

            if (player)
            {
                Debug.Log("Grind object found player");
            }
            else
            {
                Debug.Log("Grind object couldn't find player");
            }
        }

        // TEMPORARY -- remove when this is fully integrated with obstacle spawning
        transform.Translate(new Vector3(0, 0, -moveSpeed * Time.deltaTime));

        if (transform.position.z <= -15)
        {
            if (Random.Range(0.0f, 1.0f) > 0.5f)
            {
                transform.position = new Vector3(0.8f, -0.2f, 25);
            }
            else
            {
                transform.position = new Vector3(-0.8f, -0.2f, 25);
            }
        }

        // ---

    }

    public void startPlayerGrinding()
    {
        Debug.Log("Attempting grinding start");
        player.startGrind(transform.position.x, transform.position.y + 0.63f);
    }

    public void stopPlayerGrinding()
    {
        Debug.Log("Attempting grinding stop");
        player.stopGrind();
    }

    public void killPlayer()
    {
        Debug.Log("Player hit by hazard");
        GameManager.instance.switchState("Load");
    }

    //private void OnTriggerEnter(Collider other)
    //{
    //    if (other.CompareTag("Destroy"))
    //    {
    //        //PlatformManager.Instance.RemovePlatform(gameObject);
    //        //Destroy(gameObject);

    //        transform.Translate(new Vector3(0, 0, 30));

    //        Debug.Log("Grind object hit end");
    //    }
    //}
}
