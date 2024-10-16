using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "CustomSpawnPatternManager", menuName = "ScriptableObjects/Spawn Patterns Configurer")]
public class CustomSpawnPatternManager : ScriptableObject 
{
    [System.Serializable]
    public class SpawnPattern
    {
        //unity cant properly serialize 2d arrays, so we use a 1d array and a custom inspector to display it as a 2d array
        public List<string> pattern = new(new string[12]); // Flatten 4x3 grid to a 12-element list
    }

    [HideInInspector]
    public List<SpawnPattern> spawnPatterns = new();
}
