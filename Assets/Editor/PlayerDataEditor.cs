using UnityEngine;
using UnityEditor;
using System.IO;

#if UNITY_EDITOR
//maybe move to seperate SO once config object is aware of an manages sceens / game states
//filo error happens if changes are made to this file but its not a big of issue.
[CustomEditor(typeof(ConfigObject))]
[ExecuteInEditMode]
public class PlayerDataEditor : Editor
{
    private bool isDataLoaded = false; // Flag to check if data is loaded

    public override void OnInspectorGUI()
    {
        // Draw the default inspector
        DrawDefaultInspector();

        // Only show the delete button if we are not in play mode
        if (!Application.isPlaying)
        {
            ConfigObject myScript = (ConfigObject)target;
            string saveFilePath = Application.persistentDataPath + "/saveData.bin";

            // Button to delete save file
            if (GUILayout.Button("Delete Save"))
            {
                if (File.Exists(saveFilePath))
                {
                    File.Delete(saveFilePath);
                    Debug.Log("Save file deleted.");
                    //reload 
                    PlayerSaveData.Instance.Load();
                }
                else
                {
                    Debug.LogWarning("Save file does not exist.");
                }
            }

            // Load player data if save file exists, but only load once
            if (!isDataLoaded && File.Exists(saveFilePath))
            {
                PlayerSaveData.Instance.Load();
                isDataLoaded = true; // Mark data as loaded
            }

            // Display and edit highscores
            if (PlayerSaveData.Instance.highscores.Count > 0)
            {
                GUILayout.Label("Highscores", EditorStyles.boldLabel);

                for (int i = 0; i < PlayerSaveData.Instance.highscores.Count; i++)
                {
                    HighscoreEntry entry = PlayerSaveData.Instance.highscores[i];

                    // Display each highscore entry
                    GUILayout.BeginHorizontal();
                    entry.name = EditorGUILayout.TextField("Name", entry.name);
                    entry.score = EditorGUILayout.IntField("Score", entry.score);
                    GUILayout.EndHorizontal();

                    // Update the entry in the highscores list
                    PlayerSaveData.Instance.highscores[i] = entry;
                }

                // Button to save modifications
                if (GUILayout.Button("Save Changes"))
                {
                    PlayerSaveData.Instance.Save();
                    PlayerSaveData.Instance.Load(); // Reload data after saving
                    Debug.Log("Highscore changes saved.");
                }
            }
            else
            {
                GUILayout.Label("No highscores available.");
            }
        }
        else
        {
            // Optionally, you can add a label or message indicating that the button is not available in playmode
            EditorGUILayout.HelpBox("The Delete Save button is disabled while in playmode.", MessageType.Info);
        }
    }
}
#endif
