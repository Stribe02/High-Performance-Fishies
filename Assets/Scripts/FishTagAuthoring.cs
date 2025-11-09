using Unity.Entities;
using UnityEngine;

class FishTagAuthoring : MonoBehaviour
{
    
}

class FishTagBakerBaker : Baker<FishTagAuthoring>
{
    public override void Bake(FishTagAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent<FishTag>(entity);
    }

    public struct FishTag : IComponentData
    {
    }
}
