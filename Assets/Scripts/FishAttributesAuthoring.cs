using Unity.Entities;
using UnityEngine;

class FishAttributesAuthoring : MonoBehaviour
{
    public GameObject smallFishPrefab;
    public Vector3 velocity;
    public int schoolIndex;
}

class FishBaker : Baker<FishAttributesAuthoring>
{
    public override void Bake(FishAttributesAuthoring authoring)
    {
        var entity = GetEntity(authoring, TransformUsageFlags.None);
        var fishPrefab = new FishAttributes
        {
            SmallFishPrefab = GetEntity(authoring.smallFishPrefab, TransformUsageFlags.Dynamic),
            Velocity = authoring.velocity,
            SchoolIndex = authoring.schoolIndex
            
        };
        AddComponent(entity, fishPrefab);
    }
}

public struct FishAttributes : IComponentData
{
    public Entity SmallFishPrefab;
    public Vector3 Velocity;
    public int SchoolIndex;
}