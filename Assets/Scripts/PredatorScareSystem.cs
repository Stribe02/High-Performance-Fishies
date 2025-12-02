using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

[CreateAfter(typeof(FishSchoolSpawner))]
[CreateAfter(typeof(PredatorSpawnSystem))]
partial struct PredatorScareSystem : ISystem
{
    EntityQuery query_schools;
    ComponentLookup<FishAttributes> fishAttributeLookup;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<PredatorTag>();
        query_schools = new EntityQueryBuilder(Allocator.Temp).
            WithAll<FishSchoolAttribute>().
            Build(ref state);
        fishAttributeLookup = state.GetComponentLookup<FishAttributes>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var sharkEntity = SystemAPI.GetSingletonEntity<PredatorTag>();

        fishAttributeLookup.Update(ref state);

        var ecb =
            SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton
            >().CreateCommandBuffer(state.WorldUnmanaged);

        JobHandle scareJob = new MarkSchoolsScared
        {
            ecb = ecb.AsParallelWriter(),
            fishAttributeLookups = fishAttributeLookup,
            collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld,
            pos = state.EntityManager.GetComponentData<LocalTransform>(sharkEntity).Position,
            dis = state.EntityManager.GetComponentData<AquaticAnimalAttributes>(sharkEntity).Radius
        }.ScheduleParallel(state.Dependency);
        scareJob.Complete();

        JobHandle setWeightsJob = new SetSchoolAttributesJob
        {
            ecb = ecb.AsParallelWriter(),
            scaredWeight = -1,
            defaultWeight = 1
        }.ScheduleParallel(state.Dependency);
        setWeightsJob.Complete();
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
                    //FishPrefab = fishSchoolAttribute.FishPrefab,
                    SchoolEntity = fishSchoolAttribute.SchoolEntity

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
                    //FishPrefab = fishSchoolAttribute.FishPrefab,
                    SchoolEntity = fishSchoolAttribute.SchoolEntity

                });
            }
            scared.ValueRW = false;
        }
    }
}
