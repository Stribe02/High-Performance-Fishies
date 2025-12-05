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
            HasHitWall = false,
            PosToMoveAwayFrom = float3.zero
        };
        AddComponent(entity, fishPrefab);
    }
}

public struct FishAttributes : IComponentData
{
    public float3 Velocity;
    public int SchoolIndex;
    public bool HasHitWall;
    public float3 PosToMoveAwayFrom;
}

public struct FishPrefab : IComponentData
{
    public Entity prefab;
}