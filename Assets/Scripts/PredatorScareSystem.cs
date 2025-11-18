//using System.Collections.Generic; //only used for list rn, should be replaced when optimising
using System.Linq;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

[CreateAfter(typeof(FishSchoolSpawner))]
partial struct PredatorScareSystem : ISystem
{
    //float prevCohesionWeight;
    //float prevSeparationWeight;
    float prevAlignmentWeight;
    float prevSeparationRadius;
    bool fishGotScared;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        //currently just settimg them to the default but we should find a way to temporarily keep the weights they were before being scared so we can set them agai when the shark isnt close enough
        //prevCohesionWeight = 1f;
        //prevSeparationWeight = 1f;
        prevAlignmentWeight = 1f;
        prevSeparationRadius = 2f;
        
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        NativeList<Entity> hitSchools = new NativeList<Entity>(Allocator.Temp);
        
        foreach (var (transform, shark) in SystemAPI.Query<RefRW<LocalTransform>>().WithAll<PredatorTag>().WithEntityAccess())
        {
            hitSchools = PointDistanceCheck(state.EntityManager.GetComponentData<LocalTransform>(shark).Position, 2f, ref state);

            if (!hitSchools.IsEmpty)
            {
                foreach (Entity school in hitSchools)
                {
                    var schooldata = state.EntityManager.GetComponentData<FishSchoolAttribute>(school);
                    state.EntityManager.SetComponentData<FishSchoolAttribute>(school, new FishSchoolAttribute
                    {
                        SchoolIndex = schooldata.SchoolIndex,
                        CohesionWeight = -3f,
                        SeparationWeight = -1f,
                        AlignmentWeight = prevAlignmentWeight,
                        SeparationRadius = prevSeparationRadius,
                        Fishes = schooldata.Fishes,
                        FlockSize = schooldata.FlockSize,
                        FishPrefab = schooldata.FishPrefab,
                        SchoolEntity = schooldata.SchoolEntity
                    });
                }
                fishGotScared = true;
                hitSchools.Dispose();
            }
            else if (fishGotScared)
            {
                foreach (var (fishSchoolAtt, fishSchoolEnt) in SystemAPI.Query<RefRW<FishSchoolAttribute>>().WithEntityAccess())
                {
                    var schooldata = state.EntityManager.GetComponentData<FishSchoolAttribute>(fishSchoolEnt);
                    state.EntityManager.SetComponentData<FishSchoolAttribute>(fishSchoolEnt, new FishSchoolAttribute
                    {
                        SchoolIndex = schooldata.SchoolIndex,
                        CohesionWeight = 1f,
                        SeparationWeight = 1f,
                        AlignmentWeight = 1f,
                        SeparationRadius = 2f,
                        Fishes = schooldata.Fishes,
                        FlockSize = schooldata.FlockSize,
                        FishPrefab = schooldata.FishPrefab,
                        SchoolEntity = schooldata.SchoolEntity
                    });
                }
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
        //collisionWorld.Dispose();
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

   

}
