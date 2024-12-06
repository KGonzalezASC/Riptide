using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class FlyWeightSettings : ScriptableObject
{
    public FlyWeightType type;
    public GameObject prefab;
    public abstract FlyWeight Create(Vector3 position, Quaternion quaternion);

    public virtual void OnGet(FlyWeight f) => f.gameObject.SetActive(true);
    public virtual void OnRelease(FlyWeight f) => f.gameObject.SetActive(false);

    //used in cleanup of pool 
    public virtual void OnDestroyPoolObject(FlyWeight f)
    {
        Destroy(f.gameObject);
    }
}

public enum FlyWeightType { Hazard,Coin ,GrindablePole, PowerUp, SlopedGrindablePole, FlipTarget }

