using UnityEngine;
using System.IO;
using System.Collections.Generic;

[System.Serializable]
public class PlayerSaveData
{
    // Singleton pattern with lazy initialization
    static private PlayerSaveData m_Instance;
    public static PlayerSaveData Instance => m_Instance ??= new PlayerSaveData();

    protected string saveFile = "";

    // Player data
    public int usedAccessory = -1; // Implement when we have accessories... this will be the index of the accessory used by the player.
    public List<string> characterAccessories = new List<string>(); // List of owned accessories, in the form "charName:accessoryName". leave as list instead of using int to track incase we have other unique markers
    public List<HighscoreEntry> highscores = new List<HighscoreEntry>(10);


    public float masterVolume = float.MinValue, musicVolume = float.MinValue, masterSFXVolume = float.MinValue;

    // Version control for save file
    static int s_Version = 12;

    // Used in gameplay
    public void AddAccessory(string name)
    {
        characterAccessories.Add(name);
    }

    // Inserts a score at the correct place
    public void InsertScore(int score, string name)
    {
        HighscoreEntry entry = new HighscoreEntry { score = score, name = name };
        highscores.Insert(GetScorePlace(score), entry);

        // Keep only the 10 best scores.
        while (highscores.Count > 10)
            highscores.RemoveAt(highscores.Count - 1);
    }

    // Optimized GetScorePlace method using IComparer
    public int GetScorePlace(int score)
    {
        //binary search requires a IComparer to be passed in
        int index = highscores.BinarySearch(new HighscoreEntry { score = score }, new HighscoreComparer());

        return index < 0 ? ~index : index;
    }

    //public int GetScorePlace(int score)
    //{
    //    HighscoreEntry entry = new HighscoreEntry { score = score };
    //    // Use the natural comparison (IComparable) with BinarySearch
    //    int index = highscores.BinarySearch(entry);
    //    // If the score isn't found exactly, BinarySearch returns a negative index.
    //    // Use ~index to get the insertion point.
    //    return index < 0 ? ~index : index;
    //}


    // Custom comparer for highscore entries to optimize sorting by score
    public class HighscoreComparer : IComparer<HighscoreEntry>
    {
        //
        public int Compare(HighscoreEntry x, HighscoreEntry y)
        {
            //use the natural comparison of the score
            return y.score.CompareTo(x.score); // Sorting high to low.
        }
    }

    // Constructor initializes instance and save file path
    public static void Create()
    {
        m_Instance = new PlayerSaveData(); // Properly assign the singleton instance.
        m_Instance.saveFile = Application.persistentDataPath + "/saveData.bin";

        if (File.Exists(m_Instance.saveFile))
        {
            // Load existing save data
            m_Instance.Load();
        }
        else
        {
            // Create a new save if none exists
            NewSave();
        }
    }

    public static void NewSave()
    {
        m_Instance.characterAccessories.Clear();
        m_Instance.highscores.Clear();
        m_Instance.usedAccessory = -1;
        m_Instance.Save();
    }

    public void Save()
    {
        //sort highscores before saving
        SortHighScores();

        using (BinaryWriter w = new BinaryWriter(new FileStream(saveFile, FileMode.OpenOrCreate)))
        {
            w.Write(s_Version);
            w.Write(characterAccessories.Count);
            foreach (string a in characterAccessories)
            {
                w.Write(a);
            }
            w.Write(highscores.Count);
            for (int i = 0; i < highscores.Count; ++i)
            {
                w.Write(highscores[i].name);
                w.Write(highscores[i].score);
            }

            w.Write(masterVolume);
            w.Write(musicVolume);
            w.Write(masterSFXVolume);
        }
        // Log to show that the save file was saved
        Debug.Log("file saved.");
    }

    public void SortHighScores()
    {
        highscores.Sort(new HighscoreComparer());
    }


    public void Load()
    {
        if (File.Exists(saveFile))
        {
            using (BinaryReader r = new BinaryReader(new FileStream(saveFile, FileMode.Open)))
            {
                int ver = r.ReadInt32();

                // If version is too old, reset to a new save
                if (ver < 6)
                {
                    NewSave(); // This will save a new file based on current data structure
                    return;    // Return early to avoid misaligned data reads
                }

                // Read character accessories
                characterAccessories.Clear();
                int accCount = r.ReadInt32();
                for (int i = 0; i < accCount; ++i)
                {
                    characterAccessories.Add(r.ReadString()); 
                }

                // Version 3 or higher includes high scores
                if (ver >= 3)
                {
                    highscores.Clear();
                    int scoreCount = r.ReadInt32();
                    for (int i = 0; i < scoreCount; ++i)
                    {
                        highscores.Add(new HighscoreEntry { name = r.ReadString(), score = r.ReadInt32() });
                    }
                }

                // Version 9 or higher includes volume settings
                if (ver >= 9)
                {
                    masterVolume = r.ReadSingle();
                    musicVolume = r.ReadSingle();
                    masterSFXVolume = r.ReadSingle();
                }
            }
        }
        else
        {
            // If file doesn't exist, create a new save
            NewSave();
        }

        // Log to show that the save file was loaded
        Debug.Log("Save file loaded.");

        // Print character accessories
        Debug.Log("Character Accessories: " + string.Join(", ", characterAccessories));

        // Print highscores
        //foreach (var highscore in highscores)
        //{
        //    Debug.Log($"Name: {highscore.name}, Score: {highscore.score}");
        //}

        // Print volume settings
        //Debug.Log("Master Volume: " + masterVolume);
        //Debug.Log("Music Volume: " + musicVolume);
        //Debug.Log("Master SFX Volume: " + masterSFXVolume);
        //Debug.Log("version" + s_Version);
        //print the path of the save file
        //Debug.Log("Save file path: " + saveFile);
    }
}

// Struct for highscore entries
[System.Serializable]
public struct HighscoreEntry : System.IComparable<HighscoreEntry>
{
    public string name;
    public int score;
    //natural comparison 
    public int CompareTo(HighscoreEntry other)
    {
        // Compare by score - to do implement from high to low
        return other.score.CompareTo(score);
    }
}
