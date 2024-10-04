using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlatformManager : MonoBehaviour
{
    [SerializeField]
    private GameObject sectionPrefab;
    [SerializeField] //comment attribute later on because serializing list is slow
    private List<GameObject> activePlatforms = new List<GameObject>();
    private float sectionLength;
    private const int MaxActivePlatforms = 8; //fluctuates between 7-9 which is fine for now

    //slider for gap between first and second platform
    [Range(0, 10)]
    public float gap = 1.0f; //default value is 1.0f

    public List<FlyWeightSettings> hazards;

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
    }

    public void SpawnPlatforms()
    {
        if (activePlatforms.Count >= MaxActivePlatforms) { return; }
        Vector3 lastPlatformChildPosition = activePlatforms.Count > 0
            ? activePlatforms[activePlatforms.Count - 1].transform.GetChild(0).GetComponent<MeshRenderer>().bounds.max
            : Vector3.zero;

        // Calculate the new position for the first platform
        Vector3 firstNewSectionPos = new Vector3(0, 0, lastPlatformChildPosition.z + sectionLength);
        // Calculate the new position for the second platform (which will come after the first)
        Vector3 secondNewSectionPos = firstNewSectionPos + new Vector3(0, 0, sectionLength + gap);
        GameObject firstPlatform = Instantiate(sectionPrefab, firstNewSectionPos, Quaternion.identity);
        activePlatforms.Add(firstPlatform);
        GameObject secondPlatform = Instantiate(sectionPrefab, secondNewSectionPos, Quaternion.identity);
        activePlatforms.Add(secondPlatform);

        // Optionally spawn hazards on the new platforms
        SpawnHazardOnPlatform(firstPlatform);
        SpawnHazardOnPlatform(secondPlatform);

    }

    public void SpawnInitialPlatform() //we need for system to work
    {
        GameObject firstPlatform = Instantiate(sectionPrefab, Vector3.zero, Quaternion.identity);
        activePlatforms.Add(firstPlatform);

        // Spawn hazards on the first platform
        SpawnHazardOnPlatform(firstPlatform);
    }

    // New method to handle spawning hazards on a given platform
    private void SpawnHazardOnPlatform(GameObject platform)
    {
        // Retrieve the TrackHandler component from the platform's child object
        TrackHandler trackHandler = platform.GetComponentInChildren<TrackHandler>();
        if (trackHandler != null && hazards.Count > 0)
        {
            Vector3 randomPos = trackHandler.GetRandomWorldPosition();
            // Spawn the first hazard (e.g., a Coin)
            FlyWeight coin = FlyWeightFactory.Spawn(hazards[0]);
            coin.transform.position = randomPos;
            (coin as Hazard).isIgnored = false; // reset isIgnored


            FlyWeight hazard = FlyWeightFactory.Spawn(hazards[1]);
            Vector3 randomPos2 = trackHandler.GetRandomWorldPosition();
            // Prevent the second hazard from spawning at the same position as the first one
            int maxRetries = 10; // Limit the number of attempts
            int retryCount = 0;
            while (randomPos2 == randomPos && retryCount < maxRetries)
            {
                randomPos2 = trackHandler.GetRandomWorldPosition();
                retryCount++;
            }
            if (retryCount == maxRetries)
            {
                Debug.LogWarning("Failed to find a different position for the second hazard after maximum retries.");
            }
            hazard.transform.position = randomPos2;
            (hazard as Hazard).isIgnored = false; // reset isIgnored
        }
        else
        {
            Debug.LogWarning("No valid TrackHandler or hazard available.");
        }
    }


    public void RemovePlatform(GameObject platform)
    {
        if (activePlatforms.Contains(platform))
        {
            activePlatforms.Remove(platform);
        }
    }

    public void ClearAllPlatforms()
    {
        foreach (GameObject platform in activePlatforms)
        {
            Destroy(platform);
        }
        activePlatforms.Clear();
    }
}
