using Unity.Entities;
using UnityEngine;

class FishAttributesAuthoring : MonoBehaviour
{
    public Vector3 velocity;
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
        };
        AddComponent(entity, fishPrefab);
    }
}

public struct FishAttributes : IComponentData
{
    public Vector3 Velocity;
    public int SchoolIndex;
    //public Entity FishPrefab;
}

public struct FishPrefab : IComponentData
{
    public Entity prefab;
}