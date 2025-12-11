using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

class ConfigAuthoring : MonoBehaviour
{
    [Header("Choose fish prefabs with 'entities' prefix")]
    public List<GameObject> fishPrefabList;
    public GameObject rockComponent;
    public ScheduleType scheduleType;
    public int flockSize;
    public float defaultCohesionWeight = 1f;
    public float defaultSeparationWeight = 1f;
    public float defaultAlignmentWeight = 1f;
    public float defaultSeparationRadius = 2f;
    public bool shouldSpawnRock = false;
    public int moveAwayFromWallMultiplier = 4;
    [Range(1f,100f)]
    public float cohesionStepAmountFactor = 1f;
}

class ConfigAuthoringBaker : Baker<ConfigAuthoring>
{
    public override void Bake(ConfigAuthoring authoring)
    {
        var entity = GetEntity(authoring, TransformUsageFlags.None); // config itself doesn't need to move, but the fishes do

        var prefabBuffer = AddBuffer<fishPrefabs>(entity);
        for (int i = 0; i < authoring.fishPrefabList.Count; i++) {
            prefabBuffer.Add(new fishPrefabs { fishPrefab = GetEntity(authoring.fishPrefabList[i], TransformUsageFlags.Dynamic) });
        }
        
        var config = new Config
        {
            RockComponent = GetEntity(authoring.rockComponent, TransformUsageFlags.Dynamic),
            ScheduleType = authoring.scheduleType,
            FlockSize = authoring.flockSize,
            DefaultCohesionWeight = authoring.defaultCohesionWeight,
            DefaultSeparationWeight = authoring.defaultSeparationWeight,
            DefaultAlignmentWeight = authoring.defaultAlignmentWeight,
            DefaultSeparationRadius = authoring.defaultSeparationRadius,
            MoveAwayFromWallMultiplier = authoring.moveAwayFromWallMultiplier,
            CohesionStepAmountFactor = authoring.cohesionStepAmountFactor
        };
        AddComponent(entity, new RockSpawning { ShouldSpawnRock = authoring.shouldSpawnRock });
        AddComponent(entity, config);
    }
}

struct Config : IComponentData
{
    public Entity RockComponent;
    public ScheduleType ScheduleType;
    public int FlockSize;
    public float DefaultCohesionWeight;
    public float DefaultSeparationWeight;
    public float DefaultAlignmentWeight;
    public float DefaultSeparationRadius;
    public int MoveAwayFromWallMultiplier;
    public float CohesionStepAmountFactor;
}

struct fishPrefabs : IBufferElementData
{
    public Entity fishPrefab;
} 
   
public enum ScheduleType{
        Run,
        Schedule,
        ScheduleParallel
}

public struct RockSpawning : IComponentData
{
    public bool ShouldSpawnRock;
}
