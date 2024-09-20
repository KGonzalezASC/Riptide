using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Currently on prefab but we might need manager to track instances of this and seed generation of obstacles
//consider parallaxing something 
//consider how water will look visually
//probably will chnange

public class MovePlatform : MonoBehaviour
{
    public float speed = 5f;  
    public float angleX = 0f;  

    void Update()
    {
        // Calculates the direction of movement based on the angle on the x axis
        Vector3 direction = Vector3.forward;
        transform.position -= direction * speed * Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Destroy"))
        {
            Destroy(gameObject);
        }
    }
}
