using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

class FishAttributesAuthoring : MonoBehaviour
{
    public float3 velocity;
    public int schoolIndex;
}

class FishBaker : Baker<FishAttributesAuthoring>
{
    public override void Bake(FishAttributesAuthoring authoring)
    {
        var entity = GetEntity(authoring, TransformUsageFlags.Dynamic);
        var fishPrefab = new FishAttributes
        {
            Velocity = authoring.velocity,
            SchoolIndex = authoring.schoolIndex,
            CollisionAdjust = new float3()
        };
        AddComponent(entity, fishPrefab);
    }
}

public struct FishAttributes : IComponentData
{
    public float3 Velocity;
    public int SchoolIndex;
    public float3 CollisionAdjust;
}

public struct FishPrefab : IComponentData
{
    public Entity prefab;
}