using Unity.Entities;
using UnityEngine;

class ConfigAuthoring : MonoBehaviour
{
    // Hold all prefabs
    public GameObject smallFish;
    public GameObject tallFish;
    public GameObject longFish;
    public GameObject rockComponent;
    public int numberOfSchools;
    public int flockSize;
    public float defaultCohesionWeight = 1f;
    public float defaultSeparationWeight = 1f;
    public float defaultAlignmentWeight = 1f;
    public float defaultSeparationRadius = 2f;
    public bool shouldSpawnRock = false;
}

class ConfigAuthoringBaker : Baker<ConfigAuthoring>
{
    public override void Bake(ConfigAuthoring authoring)
    {
        var entity = GetEntity(authoring, TransformUsageFlags.None); // config itself doesn't need to move, but the fishes do
        var config = new Config
        {
            SmallFish = GetEntity(authoring.smallFish, TransformUsageFlags.Dynamic),
            TallFish = GetEntity(authoring.tallFish, TransformUsageFlags.Dynamic),
            LongFish = GetEntity(authoring.longFish, TransformUsageFlags.Dynamic),
            RockComponent = GetEntity(authoring.rockComponent, TransformUsageFlags.Dynamic),
            NumberOfSchools = authoring.numberOfSchools,
            FlockSize = authoring.flockSize,
            DefaultCohesionWeight = authoring.defaultCohesionWeight,
            DefaultSeparationWeight = authoring.defaultSeparationWeight,
            DefaultAlignmentWeight = authoring.defaultAlignmentWeight,
            DefaultSeparationRadius = authoring.defaultSeparationRadius
        };
        AddComponent(entity, new RockSpawning { ShouldSpawnRock = authoring.shouldSpawnRock });
        AddComponent(entity, config);
    }
}

struct Config : IComponentData
{
    public Entity SmallFish;
    public Entity TallFish;
    public Entity LongFish;
    public Entity RockComponent;
    public int NumberOfSchools;
    public int FlockSize;
    public float DefaultCohesionWeight;
    public float DefaultSeparationWeight;
    public float DefaultAlignmentWeight;
    public float DefaultSeparationRadius;
}

public struct RockSpawning : IComponentData
{
    public bool ShouldSpawnRock;
}
