using Unity.Entities;
using UnityEngine;

class FishSchoolAttributeAuthoring : MonoBehaviour
{
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
        var entity = GetEntity(TransformUsageFlags.Dynamic);
        var fishSchool = new FishSchoolAttribute
        {
           SchoolIndex = authoring.schoolIndex,
           DefaultCohesionWeight = authoring.defaultCohesionWeight,
           DefaultSeparationWeight = authoring.defaultSeparationWeight,
           DefaultAlignmentWeight = authoring.defaultAlignmentWeight,
           DefaultSeparationRadius = authoring.defaultSeparationRadius,
           FlockSize = authoring.flockSize,
           FishColor = authoring.color
        };
        AddComponent<FishSchoolAttribute>(entity);
    }
}

public struct FishSchoolAttribute : IComponentData
{
    public int SchoolIndex;
    public float DefaultCohesionWeight;
    public float DefaultSeparationWeight;
    public float DefaultAlignmentWeight;
    public float DefaultSeparationRadius;
    public int FlockSize;
    public Color FishColor;
}
