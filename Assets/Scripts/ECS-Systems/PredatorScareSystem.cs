using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using static UnityEditor.PlayerSettings;

[CreateAfter(typeof(FishSchoolSpawner))]
[CreateAfter(typeof(PredatorSpawnSystem))]
[UpdateAfter(typeof(FishSchoolMovementSystem))]
partial struct PredatorScareSystem : ISystem
{
    ComponentLookup<FishAttributes> fishAttributeLookup;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<PredatorTag>();
        state.RequireForUpdate<Config>();
        state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
        state.RequireForUpdate<PhysicsWorldSingleton>();
        fishAttributeLookup = state.GetComponentLookup<FishAttributes>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var config = SystemAPI.GetSingleton<Config>();
        var sharkEntity = SystemAPI.GetSingletonEntity<PredatorTag>();

        fishAttributeLookup.Update(ref state);

        var ecb =
            SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton
            >().CreateCommandBuffer(state.WorldUnmanaged);

        var scareJob = new MarkSchoolsScared
        {
            ecb = ecb.AsParallelWriter(),
            fishAttributeLookups = fishAttributeLookup,
            collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld,
            pos = state.EntityManager.GetComponentData<LocalTransform>(sharkEntity).Position,
            dis = state.EntityManager.GetComponentData<AquaticAnimalAttributes>(sharkEntity).Radius
        };
        var setWeightsJob = new SetSchoolAttributesJob
        {
            ecb = ecb.AsParallelWriter(),
            scaredWeight = -1,
            defaultWeight = 1
        };

        JobHandle scareHandle = default;
        JobHandle setWeightsHandle = default;
        switch (config.ScheduleType)
        {
            case ScheduleType.Schedule:
                scareHandle = scareJob.Schedule(state.Dependency);
                setWeightsHandle = setWeightsJob.Schedule(scareHandle);
                break;
            case ScheduleType.ScheduleParallel:
                scareHandle = scareJob.ScheduleParallel(state.Dependency);
                setWeightsHandle = setWeightsJob.ScheduleParallel(scareHandle);
                break;
            case ScheduleType.Run:
                foreach (var (fishSchoolAttribute, scared, schoolEntity) in SystemAPI.Query<RefRW<FishSchoolAttribute>, RefRW<ScaredTag>>().WithOptions(EntityQueryOptions.IgnoreComponentEnabledState).WithEntityAccess())
                {
                    //Mark schools as scared if they collide
                    PointDistanceInput input = new PointDistanceInput()
                    {
                        Position = state.EntityManager.GetComponentData<LocalTransform>(sharkEntity).Position,
                        MaxDistance = state.EntityManager.GetComponentData<AquaticAnimalAttributes>(sharkEntity).Radius,
                        Filter = new CollisionFilter()
                        {
                            BelongsTo = ~0u,
                            CollidesWith = ~0u, // all 1s, so all layers, collide with everything
                            GroupIndex = 0
                        }
                    };

                    NativeList<DistanceHit> distanceHits = new NativeList<DistanceHit>(Allocator.Temp);
                    SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld.CalculateDistance(input, ref distanceHits);

                    var hitSchoolIndexes = new NativeList<int>(Allocator.TempJob);
                    foreach (var hit in distanceHits)
                    {
                        if (fishAttributeLookup.HasComponent(hit.Entity))
                        {
                            if (!hitSchoolIndexes.Contains(fishAttributeLookup.GetRefRO(hit.Entity).ValueRO.SchoolIndex))
                            {
                                hitSchoolIndexes.Add(fishAttributeLookup.GetRefRO(hit.Entity).ValueRO.SchoolIndex);
                            }
                        }
                    }
                    distanceHits.Dispose();

                    if (hitSchoolIndexes.Contains(fishSchoolAttribute.ValueRW.SchoolIndex))
                    {
                        state.EntityManager.SetComponentEnabled<ScaredTag>(schoolEntity, true);
                    }
                    hitSchoolIndexes.Dispose();

                    //Set Weights
                    if (state.EntityManager.IsComponentEnabled<ScaredTag>(schoolEntity))
                    {
                        state.EntityManager.SetComponentData<FishSchoolAttribute>(schoolEntity, new FishSchoolAttribute
                        {
                            SchoolIndex = fishSchoolAttribute.ValueRW.SchoolIndex,
                            CohesionWeight = -1,
                            SeparationWeight = -1,
                            AlignmentWeight = fishSchoolAttribute.ValueRW.AlignmentWeight,
                            SeparationRadius = fishSchoolAttribute.ValueRW.SeparationRadius,
                            FlockSize = fishSchoolAttribute.ValueRW.FlockSize,
                            SchoolEntity = fishSchoolAttribute.ValueRW.SchoolEntity,
                            FishHasHitWall = fishSchoolAttribute.ValueRO.FishHasHitWall,
                            PosToMoveAwayFrom = fishSchoolAttribute.ValueRO.PosToMoveAwayFrom
                        });
                    }
                    else if (!state.EntityManager.IsComponentEnabled<ScaredTag>(schoolEntity))
                    {
                        state.EntityManager.SetComponentData<FishSchoolAttribute>(schoolEntity, new FishSchoolAttribute
                        {
                            SchoolIndex = fishSchoolAttribute.ValueRW.SchoolIndex,
                            CohesionWeight = 1,
                            SeparationWeight = 1,
                            AlignmentWeight = fishSchoolAttribute.ValueRW.AlignmentWeight,
                            SeparationRadius = fishSchoolAttribute.ValueRW.SeparationRadius,
                            FlockSize = fishSchoolAttribute.ValueRW.FlockSize,
                            SchoolEntity = fishSchoolAttribute.ValueRW.SchoolEntity,
                            FishHasHitWall = fishSchoolAttribute.ValueRO.FishHasHitWall,
                            PosToMoveAwayFrom = fishSchoolAttribute.ValueRO.PosToMoveAwayFrom
                        });
                    }
                    state.EntityManager.SetComponentEnabled<ScaredTag>(schoolEntity, false);
                }
                break;
            default:
                break;
        }

        scareHandle.Complete();
        setWeightsHandle.Complete();
    }

    [BurstCompile]
    public partial struct MarkSchoolsScared : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ecb;
        [ReadOnly] public ComponentLookup<FishAttributes> fishAttributeLookups;
        [ReadOnly] public CollisionWorld collisionWorld;
        public float3 pos;
        public float dis;
        
        public void Execute([ChunkIndexInQuery] int index, in FishSchoolAttribute fishSchoolAttribute, in Entity fishSchool)
        {
            PointDistanceInput input = new PointDistanceInput()
            {
                Position = pos,
                MaxDistance = dis,
                Filter = new CollisionFilter()
                {
                    BelongsTo = ~0u,
                    CollidesWith = ~0u, // all 1s, so all layers, collide with everything
                    GroupIndex = 0
                }
            };

            NativeList<DistanceHit> distanceHits = new NativeList<DistanceHit>(Allocator.TempJob);
            collisionWorld.CalculateDistance(input, ref distanceHits);

            var hitSchoolIndexes = new NativeList<int>(Allocator.TempJob);
            foreach (var hit in distanceHits)
            {
                if(fishAttributeLookups.HasComponent(hit.Entity))
                {
                    if (!hitSchoolIndexes.Contains(fishAttributeLookups.GetRefRO(hit.Entity).ValueRO.SchoolIndex))
                    {
                        hitSchoolIndexes.Add(fishAttributeLookups.GetRefRO(hit.Entity).ValueRO.SchoolIndex);
                    }
                }
            }
            distanceHits.Dispose();

            if (hitSchoolIndexes.Contains(fishSchoolAttribute.SchoolIndex))
            {
                ecb.SetComponentEnabled<ScaredTag>(index, fishSchool, true);
            }
            hitSchoolIndexes.Dispose();
        }
    }

    [BurstCompile]
    [WithOptions(EntityQueryOptions.IgnoreComponentEnabledState)]
    public partial struct SetSchoolAttributesJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ecb;
        public float scaredWeight;
        public float defaultWeight;

        public void Execute([ChunkIndexInQuery] int index, in FishSchoolAttribute fishSchoolAttribute, EnabledRefRW<ScaredTag> scared, in Entity school)
        {
            if (scared.ValueRO == true) {
                ecb.SetComponent<FishSchoolAttribute>(index, school, new FishSchoolAttribute
                {
                    SchoolIndex = fishSchoolAttribute.SchoolIndex,
                    CohesionWeight = scaredWeight,
                    SeparationWeight = scaredWeight,
                    AlignmentWeight = fishSchoolAttribute.AlignmentWeight,
                    SeparationRadius = fishSchoolAttribute.SeparationRadius,
                    FlockSize = fishSchoolAttribute.FlockSize,
                    SchoolEntity = fishSchoolAttribute.SchoolEntity,
                    FishHasHitWall = fishSchoolAttribute.FishHasHitWall

                });
            } else if (scared.ValueRO == false)
            {
                ecb.SetComponent<FishSchoolAttribute>(index, school, new FishSchoolAttribute
                {
                    SchoolIndex = fishSchoolAttribute.SchoolIndex,
                    CohesionWeight = defaultWeight,
                    SeparationWeight = defaultWeight,
                    AlignmentWeight = fishSchoolAttribute.AlignmentWeight,
                    SeparationRadius = fishSchoolAttribute.SeparationRadius,
                    FlockSize = fishSchoolAttribute.FlockSize,
                    SchoolEntity = fishSchoolAttribute.SchoolEntity,
                    FishHasHitWall = fishSchoolAttribute.FishHasHitWall

                });
            }
            scared.ValueRW = false;
        }
    }
}
