using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrindableObject : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 3.5f;
    [SerializeField] public FishMovement player;
    private BoxCollider grindBox;

    private void Awake()
    {

    }

    private void OnEnable()
    {
        //player = GameObject.Find("Sphere(Clone)").GetComponent<FishMovement>();

        //if (player)
        //{
        //    Debug.Log("Grind object found player");
        //}
        //else
        //{
        //    Debug.Log("Grind object couldn't find player");
        //}

        grindBox = transform.GetChild(1).GetComponent<BoxCollider>();

        if (grindBox)
        {
            Debug.Log("Grind object found grind box");
        }
        else
        {
            Debug.Log("Grind object couldn't find grind box");
        }
    }

    // Update is called once per frame
    private void FixedUpdate()
    {
        if (!player)
        {
            player = GameObject.Find("Sphere(Clone)").GetComponent<FishMovement>();

            if (player)
            {
                Debug.Log("Grind object found player");
            }
            else
            {
                Debug.Log("Grind object couldn't find player");
            }
        }

        transform.Translate(new Vector3(0, 0, -moveSpeed * Time.deltaTime));
    }

    public void startPlayerGrinding()
    {
        player.startGrind(transform.position.x);
    }

    public void stopPlayerGrinding()
    {
        player.stopGrind();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Destroy"))
        {
            //PlatformManager.Instance.RemovePlatform(gameObject);
            //Destroy(gameObject);

            transform.Translate(new Vector3(0, 0, 30));

            Debug.Log("Grind object hit end");
        }
    }
}
