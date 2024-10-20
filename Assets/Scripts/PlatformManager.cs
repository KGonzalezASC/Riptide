using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PlatformManager : MonoBehaviour
{
    [SerializeField]
    private GameObject sectionPrefab;
    //[SerializeField] //comment attribute later on because serializing list is slow
    private readonly List<GameObject> activePlatforms = new();
    private float sectionLength;
    private const int MaxActivePlatforms = 8; //fluctuates between 7-9 which is fine for now

    //slider for gap between first and second platform
    [Range(0, 10)]
    public float gap = 1.0f; //default value is 1.0f

    public List<FlyWeightSettings> hazards;
    public static PlatformManager Instance { get; private set; }

    [SerializeField]
    private GameObject emptyParentCoin;
    [SerializeField]
    private GameObject emptyParentHazard;




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


    //spawn patterns
    [SerializeField]
    private CustomSpawnPatternManager spawnPatternManager;
    //private int currentPatternIndex = 0;


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
            ? activePlatforms[^1].transform.GetChild(0).GetComponent<MeshRenderer>().bounds.max
            : Vector3.zero;

        // Calculate the new position for the first platform
        Vector3 firstNewSectionPos = new(0, 0, lastPlatformChildPosition.z + sectionLength);
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
        GameObject firstPlatform = Instantiate(sectionPrefab, new Vector3(0, 0, 25), Quaternion.identity);
        activePlatforms.Add(firstPlatform);
        // Spawn hazards on the first platform
        SpawnHazardOnPlatform(firstPlatform);
    }

    // Method to handle spawning hazards on a given platform
    private void SpawnHazardOnPlatform(GameObject platform)
    {
        // Retrieve the TrackHandler component from the platform's child object
        TrackHandler trackHandler = platform.GetComponentInChildren<TrackHandler>();

        if (trackHandler != null && hazards.Count > 0)
        {
            // Generate a random value to determine what to spawn
            float randomValue = Random.Range(0f, 1f);

            if (randomValue < 0.3f) // 30% chance for coin pair
            {
                //SpawnCoinPair(trackHandler);
                SpawnGrindingPole(trackHandler);
            }
            else if (randomValue < 0.65f) // 35% chance for coin line (30% + 35% = 65%)
            {
                SpawnCoinLine(trackHandler);
            }
            else if (randomValue < 0.82f) // 17% chance for hazard lines
            {
                SpawnHazardLines(trackHandler);
            }
            else if (randomValue < 0.93f) // 11% chance for dangerous non-pattern (82% + 11% = 93%)
            {
                SpawnDangerousNonPatternRandom(trackHandler);
            }
            else if (randomValue < 0.98f) // 5% chance for hazard lines random (93% + 5% = 98%)
            {
                SpawnHazardLinesRandom(trackHandler);
            }
            else // 2% chance for grinding pole pattern (smallest chance)
            {
                SpawnCoinPair(trackHandler);
                //Debug.Log("Attempting to spawn grind rail");
            }
        }
        else
        {
            Debug.LogWarning("No valid TrackHandler or hazard available.");
        }
    }





    //used as helper method for spawning hazards
    private void SpawnCoinAtPosition(Vector3 position, TrackHandler t)
    {
        if (!t.occupiedPositions.Contains(position))
        {
            FlyWeight coin = FlyWeightFactory.Spawn(hazards[0]);
            coin.transform.position = position;
            (coin as Hazard).isIgnored = false; // reset isIgnored
            t.occupiedPositions.Add(position); // Mark this position as occupied
            coin.transform.SetParent(emptyParentCoin.transform);

        }
        else
        {
            Debug.LogWarning("Position already occupied, skipping spawn.");
        }
    }

    private void SpawnHazardAtPosition(Vector3 position, TrackHandler t)
    {
        if (!t.occupiedPositions.Contains(position))
        {
            FlyWeight hazard = FlyWeightFactory.Spawn(hazards[1]);
            hazard.transform.position = position;
            (hazard as Hazard).isIgnored = false;
            t.occupiedPositions.Add(position); // Mark this position as occupied
            hazard.transform.SetParent(emptyParentHazard.transform);
        }
        else
        {
            Debug.LogWarning("Position already occupied, skipping spawn.");
        }
    }
    // Helper method to find a unique position for a hazard in occupied positions

    private Vector3 FindUniquePosition(TrackHandler trackHandler, Vector3 initialPos, int maxRetries)
    {
        Vector3 position = initialPos;
        int retryCount = 0;

        while (trackHandler.occupiedPositions.Contains(position) && retryCount < maxRetries)
        {
            (position, _, _) = trackHandler.GetRandomWorldPosition();
            retryCount++;
        }

        if (retryCount == maxRetries)
        {
            Debug.LogWarning("Failed to find a different position after maximum retries.");
        }

        return position;
    }

    //coins and a hazard 
    private void SpawnCoinPair(TrackHandler trackHandler)
    {
        // Get a random position and spawn the first coin
        var (randomPos, obstacleRowIndex, obstacleColumnIndex) = trackHandler.GetRandomWorldPosition();
        SpawnCoinAtPosition(randomPos, trackHandler);

        // Calculate the next index while ensuring it wraps around correctly
        int nextIndex = (obstacleRowIndex + 1) % trackHandler.obstaclePositions.Length; // Use obstaclePositions.Length to stay within bounds
        var nextPos = trackHandler.GetWorldPosition(trackHandler.obstaclePositions[nextIndex], obstacleColumnIndex);
        SpawnCoinAtPosition(nextPos, trackHandler);

        // Spawn a hazard in a different position using spawn hazard at position with unique position
        var hazardPos = FindUniquePosition(trackHandler, nextPos, 10); // Ensure the hazard position is unique
        SpawnHazardAtPosition(hazardPos, trackHandler);
    }

    //2 coin rows and a singular hazard
    private void SpawnCoinLine(TrackHandler trackHandler)
    {
        // Pick a random column from trackHandler obstacle positions
        int randomColumn = Random.Range(0, trackHandler.itemsPerRow);

        // Loop through each row in trackHandler obstacle positions except for the last one
        for (int i = 0; i < trackHandler.obstaclePositions.Length - 1; i++)
        {
            var randomPos = trackHandler.GetWorldPosition(trackHandler.obstaclePositions[i], randomColumn);
            SpawnCoinAtPosition(randomPos, trackHandler);
        }

        // Handle the last position with a 50/50 chance of being a coin or hazard
        var lastRandomPos = trackHandler.GetWorldPosition(trackHandler.obstaclePositions[^1], randomColumn);

        // Determine whether to spawn a coin or a hazard
        if (Random.Range(0f, 1f) < 0.5f) // 50% chance for a coin
        {
            // Check if the last position is already occupied
            if (!trackHandler.occupiedPositions.Contains(lastRandomPos))
            {
                SpawnCoinAtPosition(lastRandomPos, trackHandler);
            }
            else
            {
                // If the last position is occupied, find a new position for the coin
                lastRandomPos = FindUniquePosition(trackHandler, lastRandomPos, 10);
                SpawnCoinAtPosition(lastRandomPos, trackHandler);
            }

            // Spawn a hazard in a different position using spawn hazard at position with unique position
            var hazardPos = FindUniquePosition(trackHandler, lastRandomPos, 10); // Ensure the hazard position is unique
            SpawnHazardAtPosition(hazardPos, trackHandler);
        }
        else // Spawn a hazard instead
        {
            // Check if the last position is already occupied
            if (!trackHandler.occupiedPositions.Contains(lastRandomPos))
            {
                SpawnHazardAtPosition(lastRandomPos, trackHandler);
            }
            else
            {
                // If the last position is occupied, find a new position for the hazard
                var hazardPos = FindUniquePosition(trackHandler, lastRandomPos, 10); // Ensure the hazard position is unique
                SpawnHazardAtPosition(hazardPos, trackHandler);
            }
        }
    }

    private void SpawnDangerousNonPatternRandom(TrackHandler trackHandler)
    {
        // Pick a random column from trackHandler obstacle positions
        int randomColumn = Random.Range(0, trackHandler.itemsPerRow);

        // Loop through each row in trackHandler obstacle positions, but stop at length - 1
        for (int i = 0; i < trackHandler.obstaclePositions.Length - 1; i++)
        {
            var randomPos = trackHandler.GetWorldPosition(trackHandler.obstaclePositions[i], randomColumn);

            // Determine whether to spawn a coin or hazard
            if (Random.Range(0f, 1f) < 0.5f) // 50% chance for a coin
            {
                SpawnCoinAtPosition(randomPos, trackHandler);
            }
            else // Spawn a hazard instead
            {
                SpawnHazardAtPosition(randomPos, trackHandler);
            }
        }

        // Ensure a hazard is still spawned in a unique position
        var (randomPos2, _, _) = trackHandler.GetRandomWorldPosition();

        // Use FindUniquePosition to find a unique position for the second hazard
        randomPos2 = FindUniquePosition(trackHandler, randomPos2, 10);

        // Spawn the hazard at the unique position using SpawnHazardAtPosition
        SpawnHazardAtPosition(randomPos2, trackHandler);
    }

    private void SpawnHazardLines(TrackHandler trackHandler)
    {
        int randomColumn = Random.Range(0, trackHandler.itemsPerRow);
        for (int column = 0; column < trackHandler.itemsPerRow; column++)
        {
            // If it's not the randomly selected column, spawn hazards in every row
            if (column != randomColumn)
            {
                // Iterate through each row in the current column to spawn hazards
                for (int row = 0; row < trackHandler.obstaclePositions.Length; row++)
                {
                    var hazardPosition = trackHandler.GetWorldPosition(trackHandler.obstaclePositions[row], column);
                    SpawnHazardAtPosition(hazardPosition, trackHandler);
                }
            }
        }

        // Check for a 20% chance to spawn a full column of coins in the selected column
        float coinSpawnChance = Random.Range(0f, 1f);
        if (coinSpawnChance < 0.2f) // 20% chance
        {
            // Iterate through each row to spawn coins in the selected column
            for (int row = 0; row < trackHandler.obstaclePositions.Length; row++)
            {
                var coinPosition = trackHandler.GetWorldPosition(trackHandler.obstaclePositions[row], randomColumn);
                SpawnCoinAtPosition(coinPosition, trackHandler);
            }
        }
    }

    //pick a random row and fill up that column with hazards
    private void SpawnHazardLinesRandom(TrackHandler trackHandler)
    {
        // Pick a random row from trackHandler obstacle positions
        int randomRow = Random.Range(0, trackHandler.obstaclePositions.Length);
        for (int column = 0; column < trackHandler.itemsPerRow; column++)
        {
            var hazardPosition = trackHandler.GetWorldPosition(trackHandler.obstaclePositions[randomRow], column);
            // Give a random chance to spawn a coin above the hazard (e.g., 30% chance)
            float coinSpawnChance = Random.Range(0f, 1f);
            if (coinSpawnChance < 0.3f) // 30% chance
            {
                var coinPosition = hazardPosition + new Vector3(0, 2f, 0); // Adjust Y-axis as needed
                SpawnCoinAtPosition(coinPosition, trackHandler); // Use the SpawnCoinAtPosition method
            }
            SpawnHazardAtPosition(hazardPosition, trackHandler);
        }
    }

    //use CustomSpawnPatternManager to spawn hazards rows are reversed to match gui in editor
    private void SpawnPatternOnPlatform(TrackHandler trackHandler, int patternIndex)
    {
        if (spawnPatternManager == null || spawnPatternManager.spawnPatterns.Count == 0)
        {
            Debug.LogWarning("No spawn patterns available in CustomSpawnPatternManager.");
            return;
        }
        List<string> pattern = spawnPatternManager.spawnPatterns[patternIndex].pattern;
        // Check if the pattern is valid
        if (pattern is null)
        {
            Debug.LogError("Invalid spawn pattern");
            return;
        }
        int numRows = 4;
        int numCols = 3;
        for (int row = 0; row < numRows; row++)
        {
            // Reverse row mapping: Last row in the array corresponds to first row on the platform
            int reversedRow = numRows - 1 - row;
            for (int col = 0; col < numCols; col++)
            {
                int index = reversedRow * numCols + col;

                string cellValue = pattern[index];

                if (string.IsNullOrEmpty(cellValue)) continue; // Skip empty cells
                // Get the world position for the current row and column
                Vector3 worldPosition = trackHandler.GetWorldPosition(trackHandler.obstaclePositions[row], col);
                // Spawn based on the cell value ('h' for hazard, 'c' for coin)
                if (cellValue == "h")
                {
                    SpawnHazardAtPosition(worldPosition, trackHandler);
                }
                else if (cellValue == "c")
                {
                    SpawnCoinAtPosition(worldPosition, trackHandler);
                }
            }
        }
    }

    private void SpawnGrindingPole(TrackHandler trackHandler)
    {
        // Select a random column and always use row 0 for the pole
        int randomColumn = Random.Range(0, trackHandler.itemsPerRow);
        var polePosition = trackHandler.GetWorldPosition(trackHandler.obstaclePositions[0], randomColumn);

        // Spawn and position the pole
        FlyWeight pole = FlyWeightFactory.Spawn(hazards[2]);
        MeshRenderer meshRenderer = pole.transform.GetChild(0).GetComponent<MeshRenderer>();
        float halfwayPointZ = meshRenderer.bounds.extents.z; // Halfway is the Z extents of the MeshRenderer
        pole.transform.position = polePosition + new Vector3(0, 0, halfwayPointZ);
        (pole as Hazard).isIgnored = false;
        trackHandler.occupiedPositions.Add(polePosition);
        pole.transform.SetParent(emptyParentHazard.transform);

        // Spawn and position the power-up
        FlyWeight powerUp = FlyWeightFactory.Spawn(hazards[3]);
        powerUp.transform.position = polePosition + new Vector3(0, 2f, halfwayPointZ);
        (powerUp as Hazard).isIgnored = false;
        trackHandler.occupiedPositions.Add(powerUp.transform.position);
        powerUp.transform.SetParent(emptyParentCoin.transform);
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
