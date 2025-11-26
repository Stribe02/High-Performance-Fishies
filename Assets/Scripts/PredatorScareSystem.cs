//using System.Collections.Generic; //only used for list rn, should be replaced when optimising
using System.Collections;
using System.Linq;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

[CreateAfter(typeof(FishSchoolSpawner))]
partial struct PredatorScareSystem : ISystem
{
    bool fishGotScared;
    ComponentLookup<FishSchoolAttribute> schoolAttributeLookup;
    EntityQuery query_schools;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        schoolAttributeLookup = state.GetComponentLookup<FishSchoolAttribute>(true);
        
        query_schools = new EntityQueryBuilder(Allocator.Temp).
            WithAll<FishSchoolAttribute>().
            Build(ref state);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        schoolAttributeLookup.Update(ref state);
        
        var ecb =
            SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton
            >().CreateCommandBuffer(state.WorldUnmanaged);
        
        foreach (var (transform, shark) in SystemAPI.Query<RefRW<LocalTransform>>().WithAll<PredatorTag>().WithEntityAccess())
        {
            NativeList<Entity> hitSchools = new NativeList<Entity>(Allocator.Temp);
            hitSchools = PointDistanceCheck(state.EntityManager.GetComponentData<LocalTransform>(shark).Position, 2f, ref state);
            
            if (!hitSchools.IsEmpty)
            {
                NativeList<Entity> tempList = new NativeList<Entity>(Allocator.TempJob);
                tempList.CopyFrom(hitSchools);
                JobHandle changeJob = new ChangeSchoolAttributesJob
                {
                    ecb = ecb,
                    schools = tempList,
                    cw = -1f,
                    sw = -1f,
                    aw = 1f,
                    sr = 2f,
                    schoolData = schoolAttributeLookup
                }.Schedule(state.Dependency);
                changeJob.Complete();
                tempList.Dispose();
                hitSchools.Dispose();

                fishGotScared = true;

            }
            else if (fishGotScared)
            {
                NativeList<Entity> tempList = query_schools.ToEntityListAsync(Allocator.TempJob, out JobHandle test);

                JobHandle changeJob = new ChangeSchoolAttributesJob
                {
                    ecb = ecb,
                    schools = tempList,
                    cw = 1f,
                    sw = 1f,
                    aw = 1f,
                    sr = 2f,
                    schoolData = schoolAttributeLookup
                }.Schedule(test);
                changeJob.Complete();
                tempList.Dispose();
                
                fishGotScared = false;
            }   
        }
    }

    [BurstCompile]
    public NativeList<Entity> PointDistanceCheck(float3 pos, float dis, ref SystemState state)
    {
        var collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld;

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

        NativeList<DistanceHit> distanceHits = new NativeList<DistanceHit>(Allocator.Temp);
        collisionWorld.CalculateDistance(input, ref distanceHits);

        //JOB: find hit schools
        var hitSchools = new NativeList<int>(Allocator.Temp);
        foreach (var hit in distanceHits) { 
            if (state.EntityManager.HasComponent<FishAttributes>(hit.Entity))
            {
                if (!hitSchools.Contains(state.EntityManager.GetComponentData<FishAttributes>(hit.Entity).SchoolIndex))
                {
                    hitSchools.Add((state.EntityManager.GetComponentData<FishAttributes>(hit.Entity).SchoolIndex));
                }
            }
        }

        //JOB: Get hit school entities
        NativeList<Entity> fishSchoolsHit = new NativeList<Entity>(Allocator.Temp);
        foreach (var (fishSchoolAtt,fishSchoolEnt) in SystemAPI.Query<RefRO<FishSchoolAttribute>>().WithEntityAccess())
        {
            if(fishSchoolEnt != null && SystemAPI.GetComponentRO<FishSchoolAttribute>(fishSchoolEnt).IsValid)
            {
                if (hitSchools.Contains(SystemAPI.GetComponentRO<FishSchoolAttribute>(fishSchoolEnt).ValueRO.SchoolIndex) && fishSchoolsHit.IsCreated && !fishSchoolsHit.Contains(fishSchoolEnt))
                {
                    fishSchoolsHit.Add(fishSchoolEnt);
                }
            }
        }

        hitSchools.Dispose();

        return fishSchoolsHit;

    }

    [BurstCompile]
    public partial struct ChangeSchoolAttributesJob : IJobEntity
    {
        public EntityCommandBuffer ecb;
        public NativeList<Entity> schools;
        public float cw;
        public float sw;
        public float aw;
        public float sr;
        [ReadOnly]
        public ComponentLookup<FishSchoolAttribute> schoolData;
        
        public void Execute() {
            foreach (Entity school in schools)
            {
                ecb.SetComponent<FishSchoolAttribute>(school, new FishSchoolAttribute
                {
                    SchoolIndex = schoolData[school].SchoolIndex,
                    CohesionWeight = cw,
                    SeparationWeight = sw,
                    AlignmentWeight = aw,
                    SeparationRadius = sr,
                    //Fishes = schoolData[school].Fishes,
                    FlockSize = schoolData[school].FlockSize,
                    FishPrefab = schoolData[school].FishPrefab,
                    SchoolEntity = schoolData[school].SchoolEntity

                });
            }
        }

    }

    [BurstCompile]
    public partial struct ChangeSchoolAttributesJob2 : IJobEntity
    {
        public float cw;
        public float sw;
        public float aw;
        public float sr;
        public void Execute(ref FishSchoolAttribute fishSchoolAttribute)
        {
            fishSchoolAttribute.CohesionWeight = cw;
            fishSchoolAttribute.SeparationWeight = sw;
            fishSchoolAttribute.AlignmentWeight = aw;
            fishSchoolAttribute.SeparationRadius = sr;
        }

    }



}
