#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CustomSpawnPatternManager))]
public class CustomSpawnPatternManagerEditor : Editor
{
    private CustomSpawnPatternManager spawnPatternManager;
    private int selectedPatternIndex = 0; // Index of the currently selected pattern

    private void OnEnable()
    {
        spawnPatternManager = (CustomSpawnPatternManager)target;

        // Ensure at least one pattern exists
        if (spawnPatternManager.spawnPatterns.Count == 0)
        {
            spawnPatternManager.spawnPatterns.Add(new CustomSpawnPatternManager.SpawnPattern());
        }
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        GUILayout.Label("Configure Hazard (h) and Collectible (b) Patterns:");

        // Pattern selection dropdown
        string[] patternNames = new string[spawnPatternManager.spawnPatterns.Count];
        for (int i = 0; i < spawnPatternManager.spawnPatterns.Count; i++)
        {
            patternNames[i] = "Pattern " + (i + 1);
        }
        selectedPatternIndex = EditorGUILayout.Popup("Selected Pattern", selectedPatternIndex, patternNames);

        // Draw the grid for the selected pattern (now as a flat list)
        DrawPatternGrid(spawnPatternManager.spawnPatterns[selectedPatternIndex].pattern);

        GUILayout.Space(10);

        // Add and remove pattern buttons
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Add New Pattern"))
        {
            spawnPatternManager.spawnPatterns.Add(new CustomSpawnPatternManager.SpawnPattern());
            selectedPatternIndex = spawnPatternManager.spawnPatterns.Count - 1;
        }
        if (GUILayout.Button("Remove Current Pattern") && spawnPatternManager.spawnPatterns.Count > 1)
        {
            spawnPatternManager.spawnPatterns.RemoveAt(selectedPatternIndex);
            selectedPatternIndex = Mathf.Clamp(selectedPatternIndex, 0, spawnPatternManager.spawnPatterns.Count - 1);
        }
        GUILayout.EndHorizontal();

        if (GUI.changed)
        {
            EditorUtility.SetDirty(spawnPatternManager);
        }
    }

    private void DrawPatternGrid(List<string> pattern)
    {
        int numRows = 4;
        int numCols = 3;

        for (int row = 0; row < numRows; row++)
        {
            GUILayout.BeginHorizontal();
            for (int col = 0; col < numCols; col++)
            {
                int index = row * numCols + col;
                pattern[index] = GUILayout.TextField(pattern[index], GUILayout.Width(20));
            }
            GUILayout.EndHorizontal();
        }
    }
}
#endif
