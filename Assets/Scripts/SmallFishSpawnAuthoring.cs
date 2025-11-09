using Unity.Entities;
using UnityEngine;

class SmallFishAuthoring : MonoBehaviour
{
    public GameObject smallFishPrefab;
}

class FishBakerBaker : Baker<SmallFishAuthoring>
{
    public override void Bake(SmallFishAuthoring authoring)
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