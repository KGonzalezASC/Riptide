#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

[CustomEditor(typeof(PlatformManager))]
public class PlatformManagerEditor : Editor
{
    private List<string[,]> spawnPatterns = new List<string[,]>(); // List to store multiple patterns
    private int selectedPatternIndex = 0; // Index of the currently selected pattern

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        PlatformManager platformManager = (PlatformManager)target;

        // Ensure we have at least one pattern in the list
        if (spawnPatterns.Count == 0)
        {
            spawnPatterns.Add(new string[4, 3]); // Add an empty pattern
        }

        // Draw the interface for managing patterns
        GUILayout.Label("Configure Hazard (h) and Collectible (b) Patterns:");

        // Pattern Selection Dropdown
        string[] patternNames = new string[spawnPatterns.Count];
        for (int i = 0; i < spawnPatterns.Count; i++)
        {
            patternNames[i] = "Pattern " + (i + 1);
        }
        selectedPatternIndex = EditorGUILayout.Popup("Selected Pattern", selectedPatternIndex, patternNames);

        // Draw the grid for the selected pattern
        DrawPatternGrid(spawnPatterns[selectedPatternIndex]);

        GUILayout.Space(10);

        // Buttons to add and remove patterns
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Add New Pattern"))
        {
            AddNewPattern();
        }
        if (GUILayout.Button("Remove Current Pattern") && spawnPatterns.Count > 1)
        {
            RemoveCurrentPattern();
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(10);

        // Button to save the entered pattern
        if (GUILayout.Button("Save All Patterns"))
        {
            SavePatterns(platformManager);
        }
    }

    private void DrawPatternGrid(string[,] pattern)
    {
        for (int row = 0; row < 4; row++)
        {
            GUILayout.BeginHorizontal();
            for (int col = 0; col < 3; col++)
            {
                pattern[row, col] = GUILayout.TextField(pattern[row, col], GUILayout.Width(20));
            }
            GUILayout.EndHorizontal();
        }
    }

    private void AddNewPattern()
    {
        spawnPatterns.Add(new string[4, 3]); // Add a new empty pattern
        selectedPatternIndex = spawnPatterns.Count - 1; // Select the newly added pattern
    }

    private void RemoveCurrentPattern()
    {
        if (spawnPatterns.Count > 1)
        {
            spawnPatterns.RemoveAt(selectedPatternIndex); // Remove the current pattern if there are multiple patterns
            selectedPatternIndex = Mathf.Clamp(selectedPatternIndex, 0, spawnPatterns.Count - 1); // Adjust the selection index
        }
        else
        {
            // Clear the last remaining pattern
            ClearPattern(spawnPatterns[0]);
        }
    }

    private void ClearPattern(string[,] pattern)
    {
        for (int row = 0; row < 4; row++)
        {
            for (int col = 0; col < 3; col++)
            {
                pattern[row, col] = ""; // Clear each cell in the pattern by setting it to an empty string
            }
        }
    }


    private void SavePatterns(PlatformManager platformManager)
    {
        
    }
}
#endif
