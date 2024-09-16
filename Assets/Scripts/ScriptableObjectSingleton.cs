using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System;

// Singleton pattern for ScriptableObjects loaded from Addressables

public abstract class ScriptableObjectSingleton<T> : ScriptableObject where T : ScriptableObjectSingleton<T>
{
    private static T instance;
    private static bool isLoading;
    private static AsyncOperationHandle<T>? loadOperation;

    // Event or callback to notify when the singleton is ready
    public static event Action<T> OnSingletonReady;

    public static bool IsReady => instance != null;

    public static void LoadInstanceAsync()
    {
        if (instance == null && !isLoading)
        {
            isLoading = true;

            // Asynchronously load the asset
            loadOperation = Addressables.LoadAssetAsync<T>(typeof(T).Name);

            // Handle completion asynchronously
            loadOperation.Value.Completed += handle =>
            {
                isLoading = false;

                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    instance = handle.Result;

                    // Optionally handle errors or log success
                    Debug.Log($"Singleton<{typeof(T).Name}> loaded successfully!");

                    // Notify subscribers that the singleton is ready
                    OnSingletonReady?.Invoke(instance);
                }
                else
                {
                    Debug.LogError($"Failed to load Singleton<{typeof(T).Name}> from Addressables.");
                }
            };
        }
    }

    // Access instance safely after it's loaded
    public static T Instance
    {
        get
        {
            if (instance == null)
            {
                Debug.LogError($"{typeof(T).Name} is not loaded yet! Call LoadInstanceAsync() first.");
            }

            return instance;
        }
    }

    // Optionally release the loaded Addressable when done
    public static void ReleaseInstance()
    {
        if (instance != null && loadOperation.HasValue)
        {
            Addressables.Release(loadOperation.Value);
            instance = null;
        }
    }
}
