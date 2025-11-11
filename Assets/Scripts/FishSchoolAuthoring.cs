using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

class FishSchoolAttributeAuthoring : MonoBehaviour
{
    public GameObject fishPrefab;
    public int schoolIndex;
    public int flockSize;
    public float cohesionWeight = 1f;
    public float separationWeight = 1f;
    public float alignmentWeight = 1f;
    public float separationRadius = 2f;
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
           CohesionWeight = authoring.cohesionWeight,
           SeparationWeight = authoring.separationWeight,
           AlignmentWeight = authoring.alignmentWeight,
           SeparationRadius = authoring.separationRadius,
           FlockSize = authoring.flockSize,
        };
        AddComponent(entity, fishSchool);
    }
}

public struct FishSchoolAttribute : IComponentData
{
    public Entity FishPrefab;
    public int SchoolIndex;
    public float CohesionWeight;
    public float SeparationWeight;
    public float AlignmentWeight;
    public float SeparationRadius;
    public int FlockSize;
}
