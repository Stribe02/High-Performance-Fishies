using Unity.Entities;
using UnityEngine;

class wallTagAuthoring : MonoBehaviour
{
    
}

class wallTagBaker : Baker<wallTagAuthoring>
{
    public override void Bake(wallTagAuthoring authoring)
    {
        var entity = GetEntity(authoring, TransformUsageFlags.None);
        AddComponent(entity, new WallTag());
    }
}

public struct WallTag : IComponentData
{
}