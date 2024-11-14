using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
//using System.Runtime.Remoting.Services;
using Unity.VisualScripting;
using UnityEngine;
using Random = UnityEngine.Random;

public class PlatformManager : MonoBehaviour
{
    [SerializeField]
    private GameObject sectionPrefab;
    //[SerializeField] //comment attribute later on because serializing list is slow
    public readonly List<GameObject> activePlatforms = new();
    private float sectionLength;
    private const int MaxActivePlatforms = 9; 

    //slider for gap between first and second platform
    [Range(0, 10)]
    public float gap = 1.0f; //default value is 1.0f

    public List<FlyWeightSettings> hazards;
    public static PlatformManager Instance { get; private set; }

    [SerializeField]
    private GameObject emptyParentCoin;
    [SerializeField]
    private GameObject emptyParentHazard;


    //enum for type of plaform Spawn
    public enum PlatformType
    {
        SafePattern,
        HazardPattern,
        GrindingPattern,
    }
    private readonly Dictionary<PlatformType, List<Func<TrackHandler, PlatformType>>> platformPatterns;
    private readonly Queue<Func<TrackHandler, PlatformType>> lastThreeActions = new(3); // Store the last three patterns
    public PlatformManager()
    {
      platformPatterns = new Dictionary<PlatformType, List<Func<TrackHandler, PlatformType>>>
      {
        { PlatformType.SafePattern, new List<Func<TrackHandler,PlatformType>>
            {
                SpawnCoinPair,
                SpawnCoinLine,
                SpawnCoinLineAllRows
            }
        },
        { PlatformType.HazardPattern, new List<Func<TrackHandler,PlatformType>>
            {
                SpawnDangerousNonPatternRandom,
                SpawnHazardLines,
                SpawnHazardRow, // Named method reference
                trackHandler => SpawnPatternOnPlatform(trackHandler, Random.Range(0, spawnPatternManager.spawnPatterns.Count))
            }
        },
        { PlatformType.GrindingPattern, new List<Func<TrackHandler,PlatformType>>
            {
                trackHandler => SpawnGrindingPole(trackHandler),
                trackHandler => SpawnGrindingPolesCoin(trackHandler),
                //trackHandler => SpawnGrindRailCenter(trackHandler),
                trackHandler => SpawnGrindRailSloped(trackHandler) // Just method 1 but sloped. Please feel free to edit
            }
        }
      };
    }


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

        // Check if any of the last two actions were Grinding Patterns
        bool anyGrindingPattern = lastThreeActions.Any(action => IsGrindingPattern(action));

        // Set gap based on the presence of any Grinding Pattern in the last two actions
        float adjustedGap = anyGrindingPattern ? 2f : gap;


        Vector3 firstNewSectionPos = new(0, 0, lastPlatformChildPosition.z + sectionLength);
        Vector3 secondNewSectionPos = firstNewSectionPos + new Vector3(0, 0, sectionLength + adjustedGap);
        GameObject firstPlatform = Instantiate(sectionPrefab, firstNewSectionPos, Quaternion.identity);
        activePlatforms.Add(firstPlatform);
        GameObject secondPlatform = Instantiate(sectionPrefab, secondNewSectionPos, Quaternion.identity);
        activePlatforms.Add(secondPlatform);

        //third platform
        // Calculate the new position for the third platform (which will come after the second)
        Vector3 thirdNewSectionPos = secondNewSectionPos + new Vector3(0, 0, sectionLength + adjustedGap);
        GameObject thirdPlatform = Instantiate(sectionPrefab, thirdNewSectionPos, Quaternion.identity);
        activePlatforms.Add(thirdPlatform);

        SpawnHazardOnPlatform(firstPlatform);
        SpawnHazardOnPlatform(secondPlatform);
        SpawnHazardOnPlatform(thirdPlatform);
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
        TrackHandler trackHandler = platform.GetComponentInChildren<TrackHandler>();

        if (trackHandler == null || hazards.Count == 0)
        {
            Debug.LogWarning("No valid TrackHandler or hazard available.");
            return;
        }

        Func<TrackHandler, PlatformType> selectedAction = null;
        float randomValue = Random.Range(0f, 1f);

        Func<TrackHandler, PlatformType> GetRandomAction(PlatformType type)
        {
            var validMethods = platformPatterns[type];
            return validMethods[Random.Range(0, validMethods.Count)];
        }

        Func<TrackHandler, PlatformType> GetRandomActionExcluded(PlatformType type, Func<TrackHandler, PlatformType> excludeMethod)
        {
            var validMethods = platformPatterns[type].Where(action => action != excludeMethod).ToList();
            return validMethods[Random.Range(0, validMethods.Count)];
        }

        bool hasTwoGrindingActions = lastThreeActions.Count > 1 &&
                                      IsGrindingPattern(lastThreeActions.ElementAt(0)) &&
                                      IsGrindingPattern(lastThreeActions.ElementAt(1));


        bool isMostRecentGrindingPattern = lastThreeActions.Count > 0 && IsGrindingPattern(lastThreeActions.Peek());

        //bool to check if any of the last two actions were grinding patterns
        bool anyGrindingPattern = lastThreeActions.Any(action => IsGrindingPattern(action));


        selectedAction = randomValue < 0.38f ? GetRandomAction(PlatformType.SafePattern) :
                         randomValue < 0.76f ? GetRandomActionExcluded(PlatformType.HazardPattern, SpawnHazardRow) :
                         GetRandomAction(PlatformType.GrindingPattern);

        if (hasTwoGrindingActions)
        {
            //if two grinding patterns in a row, chance a safe pattern or hazard pattern with row of hazards
            selectedAction = randomValue < 0.73f ? GetRandomActionExcluded(PlatformType.HazardPattern, SpawnHazardRow) :
                                                   GetRandomAction(PlatformType.SafePattern);
            goto SectionComplete;

        }
        else if (isMostRecentGrindingPattern)
        {
            //if most recent action was grinding pattern, chance a safe pattern or another grinding pattern
            float xorReroll = Random.Range(0f, 1f);
            selectedAction = xorReroll < 0.78f ? GetRandomAction(PlatformType.GrindingPattern) :
                                                 GetRandomAction(PlatformType.SafePattern);

            goto SectionComplete;
        }
        else {
            //if no grinding patterns in last two actions, chance all patterns (allows for coin hazard row stuff just fine without grind into hazard row)
            selectedAction = randomValue < 0.32f ? GetRandomAction(PlatformType.SafePattern) :
                             randomValue < 0.78f ? GetRandomAction(PlatformType.HazardPattern) :
                             GetRandomAction(PlatformType.GrindingPattern);
        }

     SectionComplete: //label to jump to prevent grind rail into hazard row
        if (lastThreeActions.Any(action => IsGrindingPattern(action)) && selectedAction == SpawnHazardRow)
        {
            // Reselect to prevent any GrindingPattern in the queue followed by SpawnHazardRow
            selectedAction = GetRandomActionExcluded(PlatformType.HazardPattern, SpawnHazardRow);
        }
        if (selectedAction != null)
        {
            TrackLastThreeActions(selectedAction);
            selectedAction(trackHandler);
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


    //coins and a hazard 
    private PlatformType SpawnCoinPair(TrackHandler trackHandler)
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

        return PlatformType.SafePattern;
    }

    //safe pattern of all coins for all rows
    private PlatformType SpawnCoinLineAllRows(TrackHandler trackHandler)
    {
        for (int i = 0; i < trackHandler.obstaclePositions.Length; i++)
        {
            for (int j = 0; j < trackHandler.itemsPerRow; j++)
            {
                var coinPosition = trackHandler.GetWorldPosition(trackHandler.obstaclePositions[i], j);
                SpawnCoinAtPosition(coinPosition, trackHandler);
            }
        }

        return PlatformType.SafePattern;
    }






    //2 coin rows and a singular hazard
    private PlatformType SpawnCoinLine(TrackHandler trackHandler)
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

        return PlatformType.SafePattern;
    }

    private PlatformType SpawnDangerousNonPatternRandom(TrackHandler trackHandler)
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

        return PlatformType.HazardPattern;
    }

    private PlatformType SpawnHazardLines(TrackHandler trackHandler)
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

        // Iterate through each row to spawn coins in the selected column
        for (int row = 0; row < trackHandler.obstaclePositions.Length; row++)
        {
            var coinPosition = trackHandler.GetWorldPosition(trackHandler.obstaclePositions[row], randomColumn);
            SpawnCoinAtPosition(coinPosition, trackHandler);
        }
        return PlatformType.HazardPattern;
    }

    //pick a random row and fill up that column with hazards
    private PlatformType SpawnHazardRow(TrackHandler trackHandler)
    {
        // Pick a random row from trackHandler obstacle positions
        int randomRow = Random.Range(0, trackHandler.obstaclePositions.Length); //trying to die less lol
        for (int column = 0; column < trackHandler.itemsPerRow; column++)
        {
            var hazardPosition = trackHandler.GetWorldPosition(trackHandler.obstaclePositions[randomRow], column);
            // Give a random chance to spawn a coin above the hazard (e.g., 30% chance)
            float coinSpawnChance = Random.Range(0f, 1f);
            if (coinSpawnChance < 0.35f) // 35% chance
            {
                var coinPosition = hazardPosition + new Vector3(0, 2f, 0); // Adjust Y-axis as needed
                SpawnCoinAtPosition(coinPosition, trackHandler); // Use the SpawnCoinAtPosition method
            }
            SpawnHazardAtPosition(hazardPosition, trackHandler);
        }

        return PlatformType.SafePattern;
    }

    //use CustomSpawnPatternManager to spawn hazards rows are reversed to match gui in editor
    private PlatformType SpawnPatternOnPlatform(TrackHandler trackHandler, int patternIndex)
    {
        List<string> pattern = spawnPatternManager.spawnPatterns[patternIndex].pattern;
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
                    //roll random chance to spawn coin above if 40%
                    var coinSpawnChance = Random.Range(0f, 1f);
                    if (coinSpawnChance < 0.4f)
                    {
                        var coinPosition = worldPosition + new Vector3(0, 2f, 0); // Adjust Y-axis as needed
                        SpawnCoinAtPosition(coinPosition, trackHandler);
                    }
                }
                else if (cellValue == "c")
                {
                    SpawnCoinAtPosition(worldPosition, trackHandler);
                    
                }
            }
        }

        return PlatformType.HazardPattern;
    }

    private PlatformType SpawnGrindingPole(TrackHandler trackHandler)
    {
        // Select a random column and always use row 0 for the pole
        int randomColumn = Random.Range(0, trackHandler.itemsPerRow);
        var polePosition = trackHandler.GetWorldPosition(trackHandler.obstaclePositions[0], randomColumn);

        // Spawn and position the pole
        FlyWeight pole = FlyWeightFactory.Spawn(hazards[2]);
        MeshRenderer meshRenderer = pole.transform.GetChild(0).GetComponent<MeshRenderer>();
        float halfwayPointZ = meshRenderer.bounds.extents.z; // Halfway is the Z extents of the MeshRenderer
        pole.transform.position = polePosition + new Vector3(0, .3f, halfwayPointZ +2f);
        pole.transform.Rotate(-3.5f, 0f, 0f, Space.World); // Rotate by -30 degrees on the X-axis in world space
        (pole as Hazard).isIgnored = false;
        trackHandler.occupiedPositions.Add(polePosition);
        pole.transform.SetParent(emptyParentHazard.transform);



        // Spawn and position the power-up
        FlyWeight powerUp = FlyWeightFactory.Spawn(hazards[3]);
        powerUp.transform.position = polePosition + new Vector3(0, 2f, halfwayPointZ-10f);
        (powerUp as Hazard).isIgnored = false;
        trackHandler.occupiedPositions.Add(powerUp.transform.position);
        powerUp.transform.SetParent(emptyParentCoin.transform);

        return PlatformType.GrindingPattern;
    }


    private PlatformType SpawnGrindingPolesCoin(TrackHandler trackHandler)
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
        // Spawn coins with increasing and then decreasing Y values, but increasing Z
        float maxY = 12f;
        float minY = 2f;
        float zIncrement = 1.5f; // Incremental increase for Z axis per coin
        int totalCoins = 10;
        float yStep = (maxY - minY) / (totalCoins / 2); // Height step for ascent/descent

        for (int i = 0; i < totalCoins; i++)
        {
            float currentY;
            if (i < totalCoins / 2)
            {
                // Ascending phase (incrementing Y)
                currentY = minY + (i * yStep);
            }
            else
            {
                // Descending phase (decrementing Y)
                currentY = maxY - ((i - totalCoins / 2) * yStep);
            }

            // Z keeps increasing even as Y ascends or descends
            float currentZ = halfwayPointZ + 4f + (i * zIncrement);

            // Calculate the coin position
            var coinPosition = polePosition + new Vector3(0, currentY / 3f, currentZ);
            SpawnCoinAtPosition(coinPosition, trackHandler);
        }
        return PlatformType.GrindingPattern;
    }

    private PlatformType SpawnGrindRailsElevated(TrackHandler trackHandler)
    {
        // Ensure there are exactly 3 columns (column 0, 1, and 2)
        int nonElevatedColumn = Random.Range(0, 2) == 0 ? 0 : 2;  // Randomly choose column 0 or 2 to be non-elevated
        int elevatedColumn = nonElevatedColumn == 0 ? 2 : 0;  // The other column will be elevated

        float elevatedYPosition = .85f;  // Elevation for the elevated grind rail
        float groundYPosition = -0.1f;      // Ground level for the non-elevated grind rail
        float zOffset = 8f;              // Z-axis offset to push one grind rail forward

        // Loop through columns 0 and 2 (skip column 1)
        for (int column = 0; column <= 2; column += 2)
        {
            // Get the world position for the grind rail
            var railPosition = trackHandler.GetWorldPosition(trackHandler.obstaclePositions[0], column);
            FlyWeight grindRail = FlyWeightFactory.Spawn(hazards[2]); // Spawn the grind rail

            // Get MeshRenderer to calculate Z extents
            MeshRenderer meshRenderer = grindRail.transform.GetChild(0).GetComponent<MeshRenderer>();
            float halfwayPointZ = meshRenderer.bounds.extents.z; // Halfway point is the Z extents of the mesh

            // Determine Y position based on whether the column is elevated or not
            float yPosition = column == elevatedColumn ? elevatedYPosition : groundYPosition;

            // Apply Z offset to the non-elevated rail only, to move it forward
            float zPositionOffset = column == nonElevatedColumn ? halfwayPointZ + zOffset : halfwayPointZ;

            // Position the grind rail with adjusted Y and Z positions, and parent it
            grindRail.transform.position = railPosition + new Vector3(0, yPosition, zPositionOffset);
            trackHandler.occupiedPositions.Add(railPosition);
            grindRail.transform.SetParent(emptyParentHazard.transform);
        }

        return PlatformType.GrindingPattern;
    }

    //spawn grind rail in center of platform
    private PlatformType SpawnGrindRailCenter(TrackHandler trackHandler)
    {
        // Select the center column for the grind rail
        int centerColumn = trackHandler.itemsPerRow / 2;
        var railPosition = trackHandler.GetWorldPosition(trackHandler.obstaclePositions[0], centerColumn);

        // Spawn and position the grind rail
        FlyWeight grindRail = FlyWeightFactory.Spawn(hazards[2]);        
        MeshRenderer meshRenderer = grindRail.transform.GetChild(0).GetComponent<MeshRenderer>();
        float halfwayPointZ = meshRenderer.bounds.extents.z; // Halfway is the Z extents of the MeshRenderer
        grindRail.transform.position = railPosition + new Vector3(0, .3f, halfwayPointZ);
        (grindRail as Hazard).isIgnored = false;
        //rotate grind rail 30 degrees on y
        grindRail.transform.Rotate(0, 10f, 0, Space.World);


        trackHandler.occupiedPositions.Add(railPosition);
        grindRail.transform.SetParent(emptyParentHazard.transform);

        return PlatformType.GrindingPattern;
    }






    private PlatformType SpawnGrindRailSloped(TrackHandler trackHandler)
    {
        // Select a random column and always use row 0 for the pole
        int randomColumn = Random.Range(0, trackHandler.itemsPerRow);
        var polePosition = trackHandler.GetWorldPosition(trackHandler.obstaclePositions[0], randomColumn);

        // Spawn and position the pole
        FlyWeight pole = FlyWeightFactory.Spawn(hazards[4]);
        MeshRenderer meshRenderer = pole.transform.GetChild(0).GetComponent<MeshRenderer>();
        float halfwayPointZ = meshRenderer.bounds.extents.z; // Halfway is the Z extents of the MeshRenderer
        pole.transform.position = polePosition + new Vector3(0, .3f, halfwayPointZ);
        pole.transform.Rotate(-2.75f, 0f, 0f, Space.World); // Rotate by -30 degrees on the X-axis in world space
        (pole as Hazard).isIgnored = false;
        trackHandler.occupiedPositions.Add(polePosition);
        pole.transform.SetParent(emptyParentHazard.transform);



        // Spawn and position the power-up
        FlyWeight powerUp = FlyWeightFactory.Spawn(hazards[3]);
        powerUp.transform.position = polePosition + new Vector3(0, 2f, halfwayPointZ - 10f);
        (powerUp as Hazard).isIgnored = false;
        trackHandler.occupiedPositions.Add(powerUp.transform.position);
        powerUp.transform.SetParent(emptyParentCoin.transform);

        return PlatformType.GrindingPattern;
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

    private void TrackLastThreeActions(Func<TrackHandler, PlatformType> func)
    {
        if (lastThreeActions.Count >= 3)
        {
            // Remove the oldest action if there are already two actions in the queue
            lastThreeActions.Dequeue();
        }
        lastThreeActions.Enqueue(func);
    }

    private bool IsGrindingPattern(Func<TrackHandler, PlatformType> action)
    {
        return platformPatterns[PlatformType.GrindingPattern].Contains(action);
    }
}
