using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//honestly probably wont need changes

public class SectionTrigger : MonoBehaviour
{
    [SerializeField]
    private GameObject section;

    //ontrigger for the section
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("DefaultSectionTrigger"))
        {
            Debug.Log("Section Triggered");
            //instantiate the section at 0,10,11.1 per prefab times 
            Instantiate(section, new Vector3(0, 8, 10.1f), Quaternion.identity);
        }
    }
}
