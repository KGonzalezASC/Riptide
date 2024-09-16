using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>
/// IGNORE THIS SCRIPT
/// </summary>

public class testAddressables : MonoBehaviour
{
    //u can also do by assetLabels 
    [SerializeField] private AssetReferenceGameObject assetReference;

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.Space))
        {
            LoadAsset();
        }
    }

    private void LoadAsset()
    {
        AsyncOperationHandle<GameObject> op = assetReference.LoadAssetAsync<GameObject>();
        op.Completed += (obj) =>
        {
            if (obj.Status == AsyncOperationStatus.Succeeded)
            {
                GameObject go = obj.Result;
                Instantiate(go);
                Debug.Log("Asset loaded successfully", go);
            }
            else {
                Debug.LogError("Failed to load asset");
            }
        };

        
    }
}
