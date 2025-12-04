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
    BufferLookup<fishPrefabs> fishPrefabsLookup;
    ComponentTypeSet fishComponents;
    
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<Config>();
        state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();

        linkedEntityGroupLookup = state.GetBufferLookup<LinkedEntityGroup>();
        fishPrefabsLookup = state.GetBufferLookup<fishPrefabs>();

        fishComponents = new ComponentTypeSet(ComponentType.ReadWrite<FishAttributes>(), ComponentType.ReadWrite<AquaticAnimalAttributes>(), ComponentType.ReadWrite<LocalTransform>());
    }

    //[BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        state.Enabled = false;
        
        var config = SystemAPI.GetSingleton<Config>();
        var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);

        fishPrefabsLookup.Update(ref state);
        fishPrefabsLookup.TryGetBuffer(SystemAPI.GetSingletonEntity<Config>(), out DynamicBuffer<fishPrefabs> fishPrefabs);
        
        for (int i = 0; i < fishPrefabs.Length; i++)
        {
            var fishSchoolEntity = state.EntityManager.CreateEntity();

            state.EntityManager.SetName(fishSchoolEntity, $"FishSchool {i}");
            state.EntityManager.AddComponent<FishSchoolAttribute>(fishSchoolEntity);
            state.EntityManager.AddComponent<ScaredTag>(fishSchoolEntity);
            state.EntityManager.SetComponentEnabled<ScaredTag>(fishSchoolEntity, false);

            DynamicBuffer<SchoolFishes> schoolBuffer = ecb.AddBuffer<SchoolFishes>(fishSchoolEntity);
            schoolBuffer.Capacity = config.FlockSize;

            //Re-getting the buffer due to structural changes
            fishPrefabsLookup.Update(ref state);
            fishPrefabsLookup.TryGetBuffer(SystemAPI.GetSingletonEntity<Config>(), out fishPrefabs);
            state.EntityManager.SetComponentData<FishSchoolAttribute>(fishSchoolEntity, new FishSchoolAttribute
                {
                    SchoolIndex = i,
                    FlockSize = config.FlockSize,
                    CohesionWeight = config.DefaultCohesionWeight,
                    SeparationWeight = config.DefaultSeparationWeight,
                    AlignmentWeight = config.DefaultAlignmentWeight,
                    SeparationRadius = config.DefaultSeparationRadius,
                    FishPrefab = fishPrefabs.ElementAt(i).fishPrefab
                }
            );

            var fishSchoolData = state.EntityManager.GetComponentData<FishSchoolAttribute>(fishSchoolEntity);

            var fishes = state.EntityManager.Instantiate(fishSchoolData.FishPrefab, fishSchoolData.FlockSize, Allocator.TempJob); //leak?

            schoolBuffer.EnsureCapacity(fishes.Length);
            schoolBuffer.AddRange(fishes.Reinterpret<SchoolFishes>());

            linkedEntityGroupLookup.Update(ref state);

            Color c = UnityEngine.Random.ColorHSV(0f, 1f, 1f, 1f, 0.5f, 1f);
            var colorc = new float4(c.r, c.g, c.b, c.a);

            var spawnJob = new SpawnFishJob
            {
                ecb = ecb.AsParallelWriter(),
                fishes = fishes,
                color = colorc,
                linkedEntityGroups = linkedEntityGroupLookup,
                fishComponents = fishComponents,
                speed = 2f,
                schoolIndex = SystemAPI.GetComponentRW<FishSchoolAttribute>(fishSchoolEntity).ValueRW.SchoolIndex
            };

            JobHandle spawnHandle = default;
            switch (config.ScheduleType)
            {
                case ScheduleType.Schedule:
                    spawnHandle = spawnJob.Schedule(state.Dependency);
                    break;
                case ScheduleType.ScheduleParallel:
                    spawnHandle = spawnJob.ScheduleParallel(state.Dependency);
                    break;
                case ScheduleType.Run:
                    state.EntityManager.AddComponent(fishes, fishComponents);

                    var ran = new Unity.Mathematics.Random(((uint)fishSchoolData.SchoolIndex) + 1);

                    foreach (var fish in fishes)
                    {
                        state.EntityManager.SetComponentData<FishAttributes>(fish, new FishAttributes
                        {
                            SchoolIndex = fishSchoolData.SchoolIndex,
                            Velocity = ran.NextFloat3(0, 1) * 2f
                        });
                        state.EntityManager.SetComponentData<AquaticAnimalAttributes>(fish, new AquaticAnimalAttributes
                        {
                            Speed = 2f,
                            Radius = state.EntityManager.GetComponentData<AquaticAnimalAttributes>(fish).Radius
                        });
                        state.EntityManager.SetComponentData<LocalTransform>(fish, new LocalTransform
                        {
                            Position = ran.NextFloat3(-10, 10),
                            Rotation = state.EntityManager.GetComponentData<LocalTransform>(fish).Rotation,
                            Scale = state.EntityManager.GetComponentData<LocalTransform>(fish).Scale
                        });

                        linkedEntityGroupLookup.Update(ref state);
                        linkedEntityGroupLookup.TryGetBuffer(fish, out DynamicBuffer<LinkedEntityGroup> linkedEnts);

                        foreach (var ent in linkedEnts)
                        {
                            ecb.SetComponent<URPMaterialPropertyBaseColor>(ent.Value, new URPMaterialPropertyBaseColor { Value = colorc });
                        }
                    }

                    fishPrefabsLookup.Update(ref state);
                    fishPrefabsLookup.TryGetBuffer(SystemAPI.GetSingletonEntity<Config>(), out fishPrefabs);

                    break;
                default:
                    break;
            }
            spawnHandle.Complete();

            fishes.Dispose();
        }
        
    }

    [BurstCompile]
    public partial struct SpawnFishJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ecb;
        [ReadOnly] public NativeArray<Entity> fishes;
        [ReadOnly] public BufferLookup<LinkedEntityGroup> linkedEntityGroups;
        public ComponentTypeSet fishComponents;
        public float4 color;
        public float speed;
        public int schoolIndex;
        public void Execute([ChunkIndexInQuery] int index, ref LocalTransform localTransform, ref FishAttributes fishAttributes, ref AquaticAnimalAttributes aquaticAttributes, ref URPMaterialPropertyBaseColor baseColor)
        {
            
            var ran = new Unity.Mathematics.Random(((uint)schoolIndex) + 1);
            ecb.AddComponent(index, fishes, in fishComponents);

            foreach (var fish in fishes)
            {
                ecb.SetComponent<FishAttributes>(index, fish, new FishAttributes
                {
                    SchoolIndex = schoolIndex,
                    Velocity = ran.NextFloat3(0,1) * 2f
                });
                ecb.SetComponent<AquaticAnimalAttributes>(index, fish, new AquaticAnimalAttributes
                {
                    Speed = speed,
                    Radius = aquaticAttributes.Radius
                });
                ecb.SetComponent<LocalTransform>(index, fish, new LocalTransform
                {
                    Position = ran.NextFloat3(-10,10),
                    Rotation = localTransform.Rotation,
                    Scale = localTransform.Scale
                });

                linkedEntityGroups.TryGetBuffer(fish, out DynamicBuffer<LinkedEntityGroup> linkedEnts);

                foreach (var ent in linkedEnts)
                {
                    ecb.SetComponent<URPMaterialPropertyBaseColor>(index, ent.Value, new URPMaterialPropertyBaseColor { Value = color });
                }  
            }
        }
    }

 }



