using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;



partial struct FishSchoolSpawner : ISystem
{
    BufferLookup<LinkedEntityGroup> linkedEntityGroupLookup;
    
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<Config>();
        state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();

        linkedEntityGroupLookup = state.GetBufferLookup<LinkedEntityGroup>();

    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        state.Enabled = false;
        
        var config = SystemAPI.GetSingleton<Config>();
        var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);

        for (int i = 0; i < config.NumberOfSchools; i++)
        {

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

            Color c = UnityEngine.Random.ColorHSV(0f, 1f, 1f, 1f, 0.5f, 1f);
            var colorc = new float4(c.r, c.g, c.b, c.a);

            var fishes = state.EntityManager.Instantiate(fishSchoolData.FishPrefab, fishSchoolData.FlockSize, Allocator.Persistent); //leak?

            schoolBuffer.EnsureCapacity(fishes.Length);
            schoolBuffer.AddRange(fishes.Reinterpret<SchoolFishes>());

            linkedEntityGroupLookup.Update(ref state);

            JobHandle spawnJob = new SpawnFishJob
            {
                ecb = ecb,
                fishes = fishes,
                color = colorc,
                linkedEntityGroups = linkedEntityGroupLookup,
                speed = 2f,
                schoolIndex = SystemAPI.GetComponentRW<FishSchoolAttribute>(fishSchoolEntity).ValueRW.SchoolIndex
            }.Schedule(state.Dependency);
            spawnJob.Complete();

            fishes.Dispose();
        }
    }

    [BurstCompile]
    public partial struct SpawnFishJob : IJobEntity
    {
        public EntityCommandBuffer ecb;
        public NativeArray<Entity> fishes;
        public BufferLookup<LinkedEntityGroup> linkedEntityGroups;
        public float4 color;
        public float speed;
        public int schoolIndex;
        public void Execute(ref LocalTransform localTransform, ref FishAttributes fishAttributes, ref AquaticAnimalAttributes aquaticAttributes, ref URPMaterialPropertyBaseColor baseColor)
        {
            var ran = new Unity.Mathematics.Random(((uint)schoolIndex) + 1);

            foreach (var fish in fishes)
            {
                ecb.AddComponent<FishAttributes>(fish, new FishAttributes
                {
                    SchoolIndex = schoolIndex,
                    Velocity = ran.NextFloat3(0,1) * 2f
                });
                ecb.AddComponent<AquaticAnimalAttributes>(fish, new AquaticAnimalAttributes
                {
                    Speed = speed,
                    Radius = aquaticAttributes.Radius
                });
                ecb.AddComponent<LocalTransform>(fish, new LocalTransform
                {
                    Position = ran.NextFloat3(-10,10),
                    Rotation = localTransform.Rotation,
                    Scale = localTransform.Scale
                });

                linkedEntityGroups.TryGetBuffer(fish, out DynamicBuffer<LinkedEntityGroup> linkedEnts);

                foreach (var ent in linkedEnts)
                {
                    ecb.SetComponent<URPMaterialPropertyBaseColor>(ent.Value, new URPMaterialPropertyBaseColor { Value = color });
                }  
            }
        }
    }

 }



