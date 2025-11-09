using Unity.Entities;
using UnityEngine;

class SmallFishBaker : MonoBehaviour
{
    public GameObject smallFishPrefab;
}

class FishBakerBaker : Baker<SmallFishBaker>
{
    public override void Bake(SmallFishBaker authoring)
    {
        var entity = GetEntity(authoring, TransformUsageFlags.None);
        var fishPrefab = new SmallFishSpawner
        {
            SmallFishPrefab = GetEntity(authoring.smallFishPrefab, TransformUsageFlags.Dynamic)
        };
        AddComponent(entity, fishPrefab);
    }
}

public struct SmallFishSpawner : IComponentData
{
    public Entity SmallFishPrefab;
}