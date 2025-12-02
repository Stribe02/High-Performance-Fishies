using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

class ConfigAuthoring : MonoBehaviour
{
    public List<GameObject> fishPrefabList;
    public int flockSize;
    public float defaultCohesionWeight = 1f;
    public float defaultSeparationWeight = 1f;
    public float defaultAlignmentWeight = 1f;
    public float defaultSeparationRadius = 2f;
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
            FlockSize = authoring.flockSize,
            DefaultCohesionWeight = authoring.defaultCohesionWeight,
            DefaultSeparationWeight = authoring.defaultSeparationWeight,
            DefaultAlignmentWeight = authoring.defaultAlignmentWeight,
            DefaultSeparationRadius = authoring.defaultSeparationRadius
        };
        AddComponent(entity, config);
    }
}

struct Config : IComponentData
{
    public int FlockSize;
    public float DefaultCohesionWeight;
    public float DefaultSeparationWeight;
    public float DefaultAlignmentWeight;
    public float DefaultSeparationRadius;
}

struct fishPrefabs : IBufferElementData
{
    public Entity fishPrefab;
}
