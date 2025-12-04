using Unity.Entities;
using UnityEngine;

class PredatorAuthoring : MonoBehaviour
{
    public GameObject prefab;
    //public float speed;
    //public float radius;
}

class PredatorAuthoringBaker : Baker<PredatorAuthoring>
{
    public override void Bake(PredatorAuthoring authoring)
    {
        var entity = GetEntity(authoring, TransformUsageFlags.None);
        AddComponent(entity, new PredatorShark
        {
            Prefab = GetEntity(authoring.prefab, TransformUsageFlags.Dynamic)
        });
    }
}

public struct PredatorShark : IComponentData
{
    public Entity Prefab;
}

public struct PredatorTag : IComponentData
{

}

