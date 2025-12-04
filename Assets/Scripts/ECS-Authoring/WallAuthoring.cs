using Unity.Entities;
using UnityEngine;

class wallAuthoring : MonoBehaviour
{
    public WallType type;
}

class wallBaker : Baker<wallAuthoring>
{
    public override void Bake(wallAuthoring authoring)
    {
        var entity = GetEntity(authoring, TransformUsageFlags.None);
        AddComponent(entity, new Wall
        {
            WType = authoring.type
        });
    }
}

public struct Wall : IComponentData
{
    public WallType WType;
}

public enum WallType
{
    Ceiling,
    Floor,
    Left,
    Right,
    Front, 
    Back
}