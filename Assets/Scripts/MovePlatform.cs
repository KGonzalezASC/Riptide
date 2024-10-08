using System.Collections;
using System.Collections.Generic;
using UnityEngine;


//Currently on prefab but we might need manager to track instances of this and seed generation of obstacles
//consider parallaxing something 
//consider how water will look visually
//probably will chnange

public class MovePlatform : MonoBehaviour
{
    public float speed = 5f;  //this needs to be consistent with hazard speed
    public float angleX = 0f;

    void Update()
    {
        //We want to make sure that the platform is moving in the right state only.
        if (GameManager.instance.topState.GetName() == "Game")
        {
            MoveInPlayState();
        }
    }

    public void MoveInPlayState() {
        Vector3 direction = Vector3.forward;
        transform.position -= speed * Time.deltaTime * direction;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Destroy"))
        {
            PlatformManager.Instance.RemovePlatform(gameObject);
            Destroy(gameObject);
        }
    }
}

