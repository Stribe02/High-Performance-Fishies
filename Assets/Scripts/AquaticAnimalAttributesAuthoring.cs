using Unity.Entities;
using UnityEngine;

class AquaticAnimalAttributesAuthoring : MonoBehaviour
{
    public float speed;
    public float radius;
}

class Baker : Baker<AquaticAnimalAttributesAuthoring>
{
    public override void Bake(AquaticAnimalAttributesAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new AquaticAnimalAttributes
        {
            Speed = authoring.speed,
            Radius = authoring.radius
        });
    }
}

public struct AquaticAnimalAttributes : IComponentData
{
    public float Speed;
    public float Radius;
}
