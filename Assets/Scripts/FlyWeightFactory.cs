using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;


public class FlyWeightFactory : MonoBehaviour
{
    [SerializeField] int defaultCapacity = 10;
    [SerializeField] int maxPoolSize = 100;
    [SerializeField] bool collectionCheck = true;


    static FlyWeightFactory instance;
    readonly Dictionary<FlyWeightType, IObjectPool<FlyWeight>> pools = new();

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public static FlyWeight Spawn(FlyWeightSettings settings) => instance.GetPoolFor(settings)?.Get();
    public static void ReturnToPool(FlyWeight f) => instance.GetPoolFor(f.settings)?.Release(f);

    IObjectPool<FlyWeight> GetPoolFor(FlyWeightSettings settings)
    {

        if (pools.TryGetValue(settings.type, out IObjectPool<FlyWeight> pool)) return pool;

        pool = new ObjectPool<FlyWeight>(
            () => settings.Create(Vector3.zero, Quaternion.identity), //to avoid method group conversion error
            settings.OnGet,
            settings.OnRelease,
            settings.OnDestroyPoolObject,
            collectionCheck,
            defaultCapacity,
            maxPoolSize
        );
        pools.Add(settings.type, pool);
        return pool;
    }

    public static void ClearPool(FlyWeightType type)
    {
        // Then clear the object pool for that type
        if (instance.pools.TryGetValue(type, out IObjectPool<FlyWeight> pool))
        {
            pool.Clear();
        }
        // First, destroy all active FlyWeight gameObject instances of the given type in the scene
        foreach (FlyWeight activeFlyWeight in FindObjectsOfType<FlyWeight>())
        {
            if (activeFlyWeight.settings.type == type)
            {
                Destroy(activeFlyWeight.gameObject); // Destroy the GameObject
            }
        }
    }
}