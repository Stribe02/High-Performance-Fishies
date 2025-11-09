using Unity.Entities;
using UnityEngine;

class PredatorAuthoring : MonoBehaviour
{
    public GameObject prefab;
}

class PredatorAuthoringBaker : Baker<PredatorAuthoring>
{
    public override void Bake(PredatorAuthoring authoring)
    {
        var entity = GetEntity(authoring, TransformUsageFlags.None);
        var sharkPrefab = new PredatorShark
        {
            Prefab = GetEntity(authoring.prefab, TransformUsageFlags.Dynamic)
        };
        AddComponent(entity, sharkPrefab);
    }
}

public struct PredatorShark : IComponentData
{
    public Entity Prefab;
}


