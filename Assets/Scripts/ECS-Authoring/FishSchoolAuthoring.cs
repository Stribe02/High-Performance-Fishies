using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

class FishSchoolAttributeAuthoring : MonoBehaviour
{
    public int schoolIndex;
    public int flockSize;
    public float cohesionWeight = 1f;
    public float separationWeight = 1f;
    public float alignmentWeight = 1f;
    public float separationRadius = 2f;
    public List<float> cohesionWeights;
    public GameObject fishPrefab;
    public GameObject SchoolObject;

}

class FishSchoolAuthoringBaker : Baker<FishSchoolAttributeAuthoring>
{
    public override void Bake(FishSchoolAttributeAuthoring authoring)
    {
        var entity = GetEntity(authoring, TransformUsageFlags.None); // school itself doesn't need to move, but the fishes do
        var fishSchool = new FishSchoolAttribute
        { 
           SchoolIndex = authoring.schoolIndex,
           CohesionWeight = authoring.cohesionWeight,
           SeparationWeight = authoring.separationWeight,
           AlignmentWeight = authoring.alignmentWeight,
           SeparationRadius = authoring.separationRadius,
           FlockSize = authoring.flockSize,
           FishPrefab = GetEntity(authoring.fishPrefab, TransformUsageFlags.Dynamic),
           SchoolEntity = GetEntity(authoring.SchoolObject, TransformUsageFlags.None),
           FishHasHitWall = false,
           PosToMoveAwayFrom = float3.zero
        };
        AddComponent(entity, fishSchool);
    }
}
[ChunkSerializable] 
public struct FishSchoolAttribute : IComponentData
{
    public int SchoolIndex;
    public float CohesionWeight;
    public float SeparationWeight;
    public float AlignmentWeight;
    public float SeparationRadius;
    public int FlockSize;
    public Entity FishPrefab;
    public Entity SchoolEntity;
    public bool FishHasHitWall;
    public float3 PosToMoveAwayFrom;
}

public struct ScaredTag : IComponentData, IEnableableComponent
{
    
}