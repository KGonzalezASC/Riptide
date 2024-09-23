using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class SectionTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("DefaultSectionTrigger"))
        {
           PlatformManager.Instance.SpawnPlatform();
        }
    }
}