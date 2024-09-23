using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlatformManager : MonoBehaviour
{
    [SerializeField]
    private GameObject sectionPrefab;
    [SerializeField] //comment attribute later on because serializing list is slow
    private List<GameObject> activePlatforms = new List<GameObject>();
    private Vector3 lastSectionPosition;
    private float sectionLength;
    private const int MaxActivePlatforms = 8; //fluctuates between 7-9 which is fine for now


    public static PlatformManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Get the length of the child plane of the section prefab which is always the first child
        Transform childPlane = sectionPrefab.transform.GetChild(0);
        MeshRenderer meshRenderer = childPlane.GetComponent<MeshRenderer>();
        sectionLength = meshRenderer.bounds.size.z; 
        lastSectionPosition = Vector3.zero;
    }
    public void SpawnPlatform()
    {
        if(activePlatforms.Count >= MaxActivePlatforms) { return; }

        Vector3 firstNewSectionPos = lastSectionPosition + new Vector3(0, 0, sectionLength / 2); // Halfway for the first section
        Vector3 secondNewSectionPos = firstNewSectionPos + new Vector3(0, 0, sectionLength); 

        GameObject firstPlatform = Instantiate(sectionPrefab, firstNewSectionPos, Quaternion.identity);
        GameObject secondPlatform = Instantiate(sectionPrefab, secondNewSectionPos, Quaternion.identity);

        activePlatforms.Add(firstPlatform);
        activePlatforms.Add(secondPlatform);

        // Update lastSectionPosition to the position of the second new section //creates gaps n such
        lastSectionPosition = secondNewSectionPos;
    }



    public void RemovePlatform(GameObject platform)
    {
        if (activePlatforms.Contains(platform))
        {
            activePlatforms.Remove(platform);
        }
    }
}


