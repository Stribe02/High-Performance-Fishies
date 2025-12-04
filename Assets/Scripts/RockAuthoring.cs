using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

class RockAuthoring : MonoBehaviour
{
    public float3 velocity;
}

class RockAuthoringBaker : Baker<RockAuthoring>
{
    public override void Bake(RockAuthoring authoring)
    {
        var entity = GetEntity(authoring, TransformUsageFlags.Dynamic);
        AddComponent(entity, new RockComponent
        {
            Velocity = authoring.velocity
        });
    }
}

struct RockComponent : IComponentData
{
    public float3 Velocity;
}
