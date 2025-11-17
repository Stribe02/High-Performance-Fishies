using System;
using System.Collections.Generic; //only used for list rn, should be replaced when optimising
using System.Linq;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEditor.PackageManager;
using UnityEditorInternal;
using static UnityEngine.Mesh;
using UnityEngine;

//[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
//[UpdateAfter(typeof(PhysicsSystemGroup))]
//[UpdateAfter(typeof(FishSchoolSpawner))]
[CreateAfter(typeof(FishSchoolSpawner))]
partial struct PredatorScareSystem : ISystem
{
    float prevCohesionWeight;
    float prevSeparationWeight;
    float prevAlignmentWeight;
    float prevSeparationRadius;
    bool fishGotScared;

    //[BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<PredatorTag>();
        state.RequireForUpdate<FishSchoolAttribute>();
        state.RequireForUpdate<PhysicsWorldSingleton>();
        //currently just settimg them to the default but we should find a way to temporarily keep the weights they were before being scared so we can set them agai when the shark isnt close enough
        prevCohesionWeight = 1f;
        prevSeparationWeight = 1f;
        prevAlignmentWeight = 1f;
        prevSeparationRadius = 2f;
        
    }

    //[BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        List<Entity> hitSchools = new List<Entity>();

        //state.EntityManager.CompleteDependencyBeforeRO<PhysicsWorldSingleton>();

        foreach (var (transform, shark) in SystemAPI.Query<RefRW<LocalTransform>>().WithAll<PredatorTag>().WithEntityAccess())
        {
            hitSchools = PointDistanceCheck(state.EntityManager.GetComponentData<LocalTransform>(shark).Position, 2f, ref state);
            //Debug.Log(hitSchools.Count);

            if (hitSchools.Any())
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

    //[BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        
    }

    public List<Entity> PointDistanceCheck(float3 pos, float dis, ref SystemState state)
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
        var hitSchools = new List<int>();
        foreach (var hit in distanceHits) { 
            if (state.EntityManager.HasComponent<FishAttributes>(hit.Entity))
            {
                if (!hitSchools.Contains(state.EntityManager.GetComponentData<FishAttributes>(hit.Entity).SchoolIndex))
                {
                    hitSchools.Add((state.EntityManager.GetComponentData<FishAttributes>(hit.Entity).SchoolIndex));
                }
            }
        }

        List<Entity> fishSchoolsHit = new List<Entity>();
        foreach (var (fishSchoolAtt,fishSchoolEnt) in SystemAPI.Query<RefRW<FishSchoolAttribute>>().WithEntityAccess())
        {
            if(!fishSchoolAtt.Equals(null) && !fishSchoolEnt.Equals(null))
            {
                if (hitSchools.Contains(fishSchoolAtt.ValueRW.SchoolIndex) && !fishSchoolsHit.Contains(fishSchoolEnt))
                {
                    fishSchoolsHit.Add(fishSchoolEnt);
                }
            }
            
        }

        return fishSchoolsHit;

    }

   

}
