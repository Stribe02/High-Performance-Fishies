using Unity.Entities;
using UnityEngine;

class FishSchoolAttributeAuthoring : MonoBehaviour
{
    public GameObject fishPrefab;
    public int schoolIndex;
    public float defaultCohesionWeight;
    public float defaultSeparationWeight;
    public float defaultAlignmentWeight;
    public float defaultSeparationRadius;
    public int flockSize;
    public Color color;
}

class FishSchoolAuthoringBaker : Baker<FishSchoolAttributeAuthoring>
{
    public override void Bake(FishSchoolAttributeAuthoring authoring)
    {
        var entity = GetEntity(authoring, TransformUsageFlags.None); // school itself doesn't need to move, but the fishes do
        var fishSchool = new FishSchoolAttribute
        { 
           FishPrefab = GetEntity(authoring.fishPrefab, TransformUsageFlags.Dynamic),
           SchoolIndex = authoring.schoolIndex,
           DefaultCohesionWeight = authoring.defaultCohesionWeight,
           DefaultSeparationWeight = authoring.defaultSeparationWeight,
           DefaultAlignmentWeight = authoring.defaultAlignmentWeight,
           DefaultSeparationRadius = authoring.defaultSeparationRadius,
           FlockSize = authoring.flockSize,
           FishColor = authoring.color,
        };
        AddComponent(entity, fishSchool);
    }
}

public struct FishSchoolAttribute : IComponentData
{
    public Entity FishPrefab;
    public int SchoolIndex;
    public float DefaultCohesionWeight;
    public float DefaultSeparationWeight;
    public float DefaultAlignmentWeight;
    public float DefaultSeparationRadius;
    public int FlockSize;
    public Color FishColor;
}
