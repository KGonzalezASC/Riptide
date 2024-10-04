using System.Collections;
using UnityEngine;


[CreateAssetMenu(fileName = "HazardSettings", menuName = "ScriptableObjects/HazardSettings")]
public class HazardSettings : FlyWeightSettings
{
    public float despawnDelay = 5f;
    public float speed = 10f;
    public float damage = 10f;

    //create override
    public override FlyWeight Create(Vector3 position, Quaternion quat)
    {
        var go = Instantiate(prefab);
        go.SetActive(false);
        go.name = prefab.name;
        var flyWeight = go.AddComponent<Hazard>();
        flyWeight.settings = this;
        flyWeight.transform.SetPositionAndRotation(position, quat);
        //insert custom logic
        return flyWeight;
    }
}
