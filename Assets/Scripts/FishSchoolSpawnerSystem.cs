using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;


partial struct FishSchoolSpawner : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<Config>();
        state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        state.Enabled = false;

        var config = SystemAPI.GetSingleton<Config>();
        var ecb =
            SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton
            >().CreateCommandBuffer(state.WorldUnmanaged);

        for (int i = 0; i < config.NumberOfSchools; i++)
        {
            // FISH SCHOOL HAVE THEIR OWN ENTITY FOR THEIR SCHOOL
            var fishSchoolEntity = state.EntityManager.CreateEntity();
            state.EntityManager.SetName(fishSchoolEntity, $"FishSchool {i}");
            state.EntityManager.AddComponent<FishSchoolAttribute>(fishSchoolEntity);
            DynamicBuffer<SchoolFishes> schoolBuffer = ecb.AddBuffer<SchoolFishes>(fishSchoolEntity);
            schoolBuffer.Capacity = config.FlockSize;
            state.EntityManager.SetComponentData<FishSchoolAttribute>(fishSchoolEntity, new FishSchoolAttribute
                {
                    SchoolIndex = i,
                    FlockSize = config.FlockSize,
                    CohesionWeight = config.DefaultCohesionWeight,
                    SeparationWeight = config.DefaultSeparationWeight,
                    AlignmentWeight = config.DefaultAlignmentWeight,
                    SeparationRadius = config.DefaultSeparationRadius,
                    FishPrefab = config.SmallFish
                }
            );
            var fishSchoolData = state.EntityManager.GetComponentData<FishSchoolAttribute>(fishSchoolEntity);

            // URPMaterialPropertyBaseColor is a component from the Entities.Graphics package 
            // that lets us set the rendered base color of a rendered entity.
            Color c = UnityEngine.Random.ColorHSV(0f, 1f, 1f, 1f, 0.5f, 1f);
            var color = new URPMaterialPropertyBaseColor { Value = new float4(c.r, c.g, c.b, c.a) };

            var fishes = state.EntityManager.Instantiate(fishSchoolData.FishPrefab, fishSchoolData.FlockSize, Allocator.Persistent); //leak?
            schoolBuffer.EnsureCapacity(fishes.Length);

            foreach (var fish in fishes)
            {
                schoolBuffer.Add(new SchoolFishes { Fish = fish });
         
                var transform = SystemAPI.GetComponentRW<LocalTransform>(fish);
                ecb.AddComponent<FishAttributes>(fish);
                
                transform.ValueRW.Position = UnityEngine.Random.insideUnitSphere * 10;
                ecb.AddComponent<AquaticAnimalAttributes>(fish);

                var speed = SystemAPI.GetComponentRW<AquaticAnimalAttributes>(fish).ValueRW.Speed = 2f;
                SystemAPI.GetComponentRW<FishAttributes>(fish).ValueRW.Velocity = UnityEngine.Random.insideUnitSphere.normalized * speed;
                SystemAPI.GetComponentRW<FishAttributes>(fish).ValueRW.SchoolIndex = i;

                // Every root entity instantiated from a prefab has a LinkedEntityGroup component, which
                // is a list of all the entities that make up the prefab hierarchy (including the root).

                // (LinkedEntityGroup is a special kind of component called a "DynamicBuffer", which is
                // a resizable array of struct values instead of just a single struct.)
                var linkedEntities = state.EntityManager.GetBuffer<LinkedEntityGroup>(fish);
                foreach (var entity in linkedEntities)
                {
                    // We want to set the URPMaterialPropertyBaseColor component only on the
                    // entities that have it, so we first check.
                    if (state.EntityManager.HasComponent<URPMaterialPropertyBaseColor>(entity.Value))
                    {
                        // Set the color of each entity that makes up the tank.
                        state.EntityManager.SetComponentData(entity.Value, color);
                    }
                }
            }

            //SystemAPI.GetComponentRW<FishSchoolAttribute>(fishSchoolEntity).ValueRW.Fishes = fishes;
            fishes.Dispose();

        }
    }
 }


