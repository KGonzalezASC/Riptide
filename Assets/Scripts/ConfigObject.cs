using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Events;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>
///  This can act as a scene independent manager meaning it can be used to load prefabs or other assets that are used across multiple scenes without being destroyed when the scene changes.
///  Use this when the same prefab is used in multiple scenes and you want to load it once and keep it loaded. or when applicable.
///  Yes there can be multiple scriptable object singletons in a project.
///  
///  treat this like the main game manager
/// </summary>


//make sure to assign by Label ConfigObject to the SO in the Addressables window
[CreateAssetMenu(fileName = "ConfigObject", menuName = "ScriptableObjects/sceneIndependantManager")]
public class ConfigObject : ScriptableObjectSingleton<ConfigObject>
{
    public static event Action OnHam;
    // an example event to trigger when the prefab is instantiated
    // have listeners in any game object in their awake method

    [SerializeField]
    private AssetReferenceT<GameObject> prefab;
    private GameObject instanceRef;

    public void InstantiatePrefab(Vector3 position, Transform parent = null)
    {
        if (prefab != null)
        {
            var op = prefab.InstantiateAsync(position, Quaternion.identity, parent);
            // Use the completed event to handle the completion of the instantiation
            op.Completed += OnPrefabInstantiated;
        }
    }

    private void OnPrefabInstantiated(AsyncOperationHandle<GameObject> obj)
    {
        if (obj.Status == AsyncOperationStatus.Succeeded)
        {
            instanceRef = obj.Result;
            OnHam?.Invoke();
        }
        else
        {
            Debug.LogError($"Failed to instantiate prefab: {obj.OperationException}");
        }
    }

    public void ReleasePrefab()
    {
        if (instanceRef != null)
        {
            //aka destroyes game object
            // Release the instance via Addressables
            Addressables.ReleaseInstance(instanceRef);
            instanceRef = null; // Clear reference after release
            Debug.Log("Prefab instance released.");
        }
        else
        {
            Debug.LogWarning("No prefab instance to release.");
        }
    }

    // Change Init to load the singleton asynchronously
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        // Load the singleton asynchronously first
        LoadInstanceAsync();
        // Listen for when the singleton is ready
        OnSingletonReady += OnConfigObjectReady;
    }

    // This method will be called once the ConfigObject is fully loaded
    private static void OnConfigObjectReady(ConfigObject configInstance)
    {
        // Now we can safely instantiate the prefab
        // TODO: HAVE THIS LOAD ALT FISH COSTUMES FOR STORE WHEN APPLICABLE:
        configInstance.InstantiatePrefab(new Vector3(0, 1, .1f));
        Debug.Log("ConfigObject is ready and prefab instantiated.");
        PlayerSaveData.Create();

        // Test save data
        PlayerSaveData.Instance.Save();

        // Insert random high score data from 100-200 if no high scores exist
        System.Random random = new System.Random();
        if (PlayerSaveData.Instance.highscores.Count == 0)
        {
            for (int i = 0; i < 10; i++)
            {
                int score = random.Next(100, 201); // Random score between 100 and 200
                string name = "Player" + i; // Unique name for each entry

                PlayerSaveData.Instance.InsertScore(score, name);
            }

            //output a log file
            Debug.Log("High scores saved.");
        }

        // Save the data again to ensure the new scores are stored
        PlayerSaveData.Instance.Save();
    }
}
