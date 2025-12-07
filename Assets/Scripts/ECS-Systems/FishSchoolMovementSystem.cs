using System.IO;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEditor.Rendering;
using UnityEngine;

[CreateAfter(typeof(FishSchoolSpawner))]
partial struct FishSchoolMovementSystem : ISystem
{

    BufferLookup<SchoolFishes> schoolFishesLookup;
    EntityQuery query_fish;
    ComponentLookup<LocalTransform> neighbourLocalTransformLookup;

    

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton
        >();
        state.RequireForUpdate<Config>();
        schoolFishesLookup = state.GetBufferLookup<SchoolFishes>();
        neighbourLocalTransformLookup = state.GetComponentLookup<LocalTransform>(true);
        query_fish = new EntityQueryBuilder(Allocator.Temp).WithAll<FishAttributes>().Build(ref state);
        

    }
    
    public void OnUpdate(ref SystemState state)
    {
        schoolFishesLookup.Update(ref state);
        neighbourLocalTransformLookup.Update(ref state);
        var ecb =
            SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton
            >().CreateCommandBuffer(state.WorldUnmanaged);

        var config = SystemAPI.GetSingleton<Config>();


        // should loop through the different school the cohesion etc. methods needs to loop through the fish of the school they get
        foreach (var (fishSchoolAttribute, fishBuffer, fishSchool) in SystemAPI
                     .Query<RefRW<FishSchoolAttribute>, DynamicBuffer<SchoolFishes>>().WithEntityAccess())
        {
            if (config.ScheduleType is ScheduleType.Schedule or ScheduleType.ScheduleParallel)
            {
                /* Cohesion Job */
                NativeArray<float3> centerOfMass = new NativeArray<float3>(1, Allocator.TempJob);
                NativeArray<int> cohesionCount = new NativeArray<int>(1, Allocator.TempJob);

                CohesionJob cohesionJob = new CohesionJob
                {
                    centerOfMass = centerOfMass,
                    cohesionCount = cohesionCount,
                    schoolIndex = fishSchoolAttribute.ValueRO.SchoolIndex
                };

                JobHandle cohesionHandle = default;
                if (config.ScheduleType == ScheduleType.Schedule)
                {
                    cohesionHandle = cohesionJob.Schedule(state.Dependency);
                }
                else
                {
                    cohesionHandle = cohesionJob.ScheduleParallel(state.Dependency);
                }

                /* Seperation Job */
                NativeArray<float3> moveAway = new NativeArray<float3>(1, Allocator.TempJob);
                NativeArray<int> seperationCount = new NativeArray<int>(1, Allocator.TempJob);
                NativeList<Entity> tempList = query_fish.ToEntityListAsync(Allocator.TempJob, out JobHandle test);

                SeparationJob separationJob = new SeparationJob
                {
                    fishes = tempList,
                    moveAway = moveAway,
                    seperationCount = seperationCount,
                    neighbourTransform = neighbourLocalTransformLookup,
                    schoolIndex = fishSchoolAttribute.ValueRO.SchoolIndex,
                    seperationRadius = fishSchoolAttribute.ValueRO.SeparationRadius
                };

                JobHandle separationHandle = default;
                if (config.ScheduleType == ScheduleType.Schedule)
                {
                    separationHandle = separationJob.Schedule(test);
                }
                else
                {
                    separationHandle = separationJob.ScheduleParallel(test);
                }

                /* AlignmentJob: */

                NativeList<Entity> tempFishList = query_fish.ToEntityListAsync(Allocator.TempJob, out JobHandle secondQuery);
                NativeArray<float3> averageVelocity = new NativeArray<float3>(1, Allocator.TempJob);
                NativeArray<int> alignmentCount = new NativeArray<int>(1, Allocator.TempJob);
                AlignmentJob alignmentJob = new AlignmentJob
                {
                    averageVelocity = averageVelocity,
                    count = alignmentCount,
                    schoolIndex = fishSchoolAttribute.ValueRO.SchoolIndex,
                    fishes = tempFishList
                };

                JobHandle alignmentHandle = default;
                if (config.ScheduleType == ScheduleType.Schedule)
                {
                    alignmentHandle = alignmentJob.Schedule(secondQuery);
                }
                else
                {
                    alignmentHandle = alignmentJob.ScheduleParallel(secondQuery);
                }

                /* MoveAwayFrom Job*/
                float3 moveAwayWallFloaty = float3.zero;
                if (fishSchoolAttribute.ValueRO.FishHasHitWall)
                {
                    JobHandle moveAwayHandle = default;
                    NativeArray<float3> moveAwayFloatyArray = new NativeArray<float3>(1, Allocator.TempJob);
                    MoveAwayFromWallJob moveAwayFromWallJob = new MoveAwayFromWallJob
                    {
                        moveAwayFloaty = moveAwayFloatyArray,
                        multiplier = config.MoveAwayFromWallMultiplier,
                        schoolIndex = fishSchoolAttribute.ValueRO.SchoolIndex
                    };
                    if (config.ScheduleType == ScheduleType.Schedule)
                    {
                        moveAwayHandle = moveAwayFromWallJob.Schedule(state.Dependency);
                    }
                    else
                    {
                        moveAwayHandle = moveAwayFromWallJob.ScheduleParallel(state.Dependency);
                    }
                    moveAwayHandle.Complete();
                    moveAwayWallFloaty = moveAwayFloatyArray[0];
                    moveAwayFloatyArray.Dispose();
                    fishSchoolAttribute.ValueRW.FishHasHitWall = false;
                }

                // make sure the jobs are completed
                // yoink the values from job
                cohesionHandle.Complete();
                alignmentHandle.Complete();
                separationHandle.Complete();

                
                UpdateFishDataJob updateFishJob = new UpdateFishDataJob
                {
                    centerOfMass = centerOfMass[0],
                    schoolIndex = fishSchoolAttribute.ValueRO.SchoolIndex,
                    cohesionCount = cohesionCount[0]-1,
                    seperationCount = seperationCount[0]-1,
                    alignmentCount = alignmentCount[0]-1,
                    moveAway = moveAway[0],
                    averageVelocity = averageVelocity[0],
                    moveAwayFloatyWall = moveAwayWallFloaty,
                    deltaTime = SystemAPI.Time.DeltaTime,
                    cohesionWeight = fishSchoolAttribute.ValueRO.CohesionWeight,
                    separationWeight = fishSchoolAttribute.ValueRO.SeparationWeight,
                    alignmentWeight = fishSchoolAttribute.ValueRO.AlignmentWeight,
                };

                JobHandle updateFishHandle = default;
                if (config.ScheduleType == ScheduleType.Schedule)
                {
                    updateFishHandle = updateFishJob.Schedule(state.Dependency);
                }
                else
                {
                    updateFishHandle = updateFishJob.ScheduleParallel(state.Dependency);
                }

                updateFishHandle.Complete();
                
                // Dispose of the Arrays used in jobs:
                centerOfMass.Dispose();
                cohesionCount.Dispose();
                moveAway.Dispose();
                tempList.Dispose();
                seperationCount.Dispose();
                averageVelocity.Dispose();
                alignmentCount.Dispose();
                tempFishList.Dispose();

            } 
            else if (config.ScheduleType == ScheduleType.Run)
            {
                foreach (var (fishAttributes, aquaData, fishTransform, fish) in SystemAPI
                             .Query<RefRW<FishAttributes>, RefRW<AquaticAnimalAttributes>, RefRW<LocalTransform>>()
                             .WithEntityAccess())
                {
                    if (fishAttributes.ValueRO.SchoolIndex == fishSchoolAttribute.ValueRO.SchoolIndex)
                    {
                        // For each loops in here
                        float3 cohesion =
                            Cohesion(ref state, fish, fishTransform.ValueRO.Position,
                                fishSchoolAttribute.ValueRO.SchoolIndex) *
                            fishSchoolAttribute.ValueRO.CohesionWeight;
                        float3 separation =
                            Separation(ref state, fish, fishTransform.ValueRO.Position,
                                fishSchoolAttribute.ValueRO.SchoolIndex,
                                fishSchoolAttribute.ValueRO.SeparationRadius) *
                            fishSchoolAttribute.ValueRO.SeparationWeight;
                        float3 alignment =
                            Alignment(ref state, fish, fishTransform.ValueRO.Position,
                                fishSchoolAttribute.ValueRO.SchoolIndex) *
                            fishSchoolAttribute.ValueRO.AlignmentWeight;

                        /* Get the fish data and apply the rules */
                        var moveAwayFloat3 = float3.zero;
                        if (fishAttributes.ValueRO.HasHitWall)
                        {
                            moveAwayFloat3 = MoveAwayFromWall(ref state, fishAttributes.ValueRO.PosToMoveAwayFrom,
                                fishSchoolAttribute.ValueRO.SchoolIndex, 4);
                            fishAttributes.ValueRW.HasHitWall = false;
                        }

                        fishAttributes.ValueRW.Velocity += cohesion + separation + alignment;
                        fishAttributes.ValueRW.Velocity =
                            ClampLength(fishAttributes.ValueRO.Velocity, aquaData.ValueRO.Speed);


                        fishTransform.ValueRW.Position +=
                            fishAttributes.ValueRO.Velocity * SystemAPI.Time.DeltaTime + moveAwayFloat3;
                        fishTransform.ValueRW.Rotation =
                            quaternion.LookRotation(fishAttributes.ValueRO.Velocity, math.up());
                    }
                }
            }
        }
    }





    [BurstCompile]
    public partial struct CohesionJob : IJobEntity
    {
        [NativeDisableParallelForRestriction]
        public NativeArray<float3> centerOfMass;
        public int schoolIndex;
        [NativeDisableParallelForRestriction]
        public NativeArray<int> cohesionCount;

        public void Execute(in LocalTransform fishTransform, in FishAttributes fishAttributes)
        {
            if (fishAttributes.SchoolIndex == schoolIndex)
            {
                var pos = new float3(fishTransform.Position.x, fishTransform.Position.y,
                    fishTransform.Position.z);
                centerOfMass[0] += pos;
                cohesionCount[0]++;
            }
        }
    }
    
    
    [BurstCompile]
    public partial struct SeparationJob : IJobEntity
    {
        [NativeDisableParallelForRestriction]
        public NativeArray<float3> moveAway;
        public int schoolIndex;
        [NativeDisableParallelForRestriction]
        public NativeList<Entity> fishes;        
        public float seperationRadius;
        [ReadOnly] public ComponentLookup<LocalTransform> neighbourTransform;
        [NativeDisableParallelForRestriction]
        public NativeArray<int> seperationCount;

        public void Execute(in LocalTransform fishTransform, in FishAttributes fishAttributes, in Entity fish)
        {
            foreach (Entity neighbourFish in fishes)
            {
                // how do you get two entities in one go?
                if (!fish.Equals(neighbourFish) && fishAttributes.SchoolIndex == schoolIndex
                                                && (math.distance(neighbourTransform.GetRefRO(neighbourFish).ValueRO.Position,
                                                    new float3(fishTransform.Position.x,
                                                        fishTransform.Position.y,
                                                        fishTransform.Position.z)) < seperationRadius))
                {
                    float3 difference = neighbourTransform.GetRefRO(neighbourFish).ValueRO.Position - new float3(fishTransform.Position.x,
                        fishTransform.Position.y, fishTransform.Position.z);
                    moveAway[0] -= difference;
                    seperationCount[0]++;
                }
            }
        }
    }
    
    

    public partial struct AlignmentJob : IJobEntity
    {
        [NativeDisableParallelForRestriction]
        public NativeArray<float3> averageVelocity;
        public int schoolIndex;
        [NativeDisableParallelForRestriction]
        public NativeArray<int> count;
        [NativeDisableParallelForRestriction]
        public NativeList<Entity> fishes;        


        public void Execute(in LocalTransform fishTransform, in FishAttributes fishAttributes, Entity fish)
        {
            foreach (Entity neighbourFish in fishes)
            {
                if (!fish.Equals(neighbourFish) && fishAttributes.SchoolIndex == schoolIndex)
                {
                    averageVelocity[0] += fishAttributes.Velocity;
                    count[0]++;
                }
            }
        }
    }
    
    [BurstCompile]
    public partial struct MoveAwayFromWallJob : IJobEntity
    {
        [NativeDisableParallelForRestriction]
        public NativeArray<float3> moveAwayFloaty;
        public int schoolIndex;
        public int multiplier;

        public void Execute(in LocalTransform fishTransform, in FishAttributes fishAttributes)
        {
            if (fishAttributes.SchoolIndex == schoolIndex)
            {
                moveAwayFloaty[0] += -multiplier * fishAttributes.PosToMoveAwayFrom -
                                  fishTransform.Position;
            }
        }
    }
    
    
    public partial struct UpdateFishDataJob : IJobEntity
    {
        public float3 centerOfMass;
        public int schoolIndex;
        public int cohesionCount;
        public int seperationCount;
        public int alignmentCount;
        public float3 moveAway;
        public float3 averageVelocity;
        public float3 moveAwayFloatyWall;
        public float deltaTime;
        public float cohesionWeight;
        public float separationWeight;
        public float alignmentWeight;

        public void Execute(ref LocalTransform fishTransform, ref FishAttributes fishAttributes, ref AquaticAnimalAttributes aquaData, Entity fish)
        {
             if (fishAttributes.SchoolIndex == schoolIndex)
             {
                 // For each loops in here
                 float3 cohesion = cohesionCount > 0 ? math.normalize((centerOfMass / cohesionCount) - fishTransform.Position) * cohesionWeight : float3.zero;
                 float3 separation = seperationCount > 0 && !moveAway.Equals(float3.zero) ?  math.normalize(moveAway / seperationCount) * separationWeight : float3.zero;
                 //float3 separation = moveAway * separationWeight;

                 float3 alignment = alignmentCount > 0 ? averageVelocity / alignmentCount : float3.zero;
                 alignment = (alignment - fishAttributes.Velocity) / 8;
                 alignment *= alignmentWeight;
                 
                 fishAttributes.Velocity += cohesion + separation + alignment + moveAwayFloatyWall;
                 fishAttributes.Velocity = clampLength(fishAttributes.Velocity, aquaData.Speed);

                 fishTransform.Position +=
                     (fishAttributes.Velocity * deltaTime);
                 
                 fishTransform.Rotation =
                     quaternion.LookRotation(fishAttributes.Velocity, math.up());
             }
        }
        // ClampMagnitude but for float3
        private float3 clampLength(float3 floaty, float maxLength)
        {
            if (math.lengthsq(floaty) > maxLength * maxLength)
            {
                float scale = maxLength / math.length(floaty);
                return floaty * scale;
            }
            return floaty;
        }
    }
    
    /* METHODS FOR IDIOMATIC FOREACH RULES */
    
    
    // Rule 1:
    [BurstCompile]
    public float3 Cohesion(ref SystemState state, Entity fishEntity, float3 fishEntityTransform, int schoolIndex)
    {
        // Rule 1: Cohesion
        float3 centerOfMass = float3.zero;
        int cohesionCount = 0;
        
        foreach (var (fishTransform, fishAttributes ,fish) in SystemAPI.Query<RefRW<LocalTransform>, RefRO<FishAttributes>>()
                     .WithEntityAccess())
        {
            if (fishAttributes.ValueRO.SchoolIndex == schoolIndex && !fishEntity.Equals(fish))
            {
                var pos = new float3(fishTransform.ValueRO.Position.x, fishTransform.ValueRO.Position.y,
                    fishTransform.ValueRO.Position.z);
                centerOfMass += pos;
                cohesionCount++;
            }
        }
        
        if (cohesionCount > 0)
        {
            centerOfMass /= cohesionCount;
            return math.normalize(centerOfMass - fishEntityTransform);
        }

        return float3.zero;
    }
    
    // Rule 2:
    [BurstCompile]
    public float3 Separation(ref SystemState state, Entity fishEntity,
        float3 fishEntityTransform, int schoolIndex, float seperationRadius)
    {
        float3 moveAway = float3.zero;
        int seperationCount = 0;
        
        
        foreach (var (fishTransform, fishAttributes ,fish) in SystemAPI.Query<RefRW<LocalTransform>, RefRO<FishAttributes>>()
                     .WithEntityAccess())
        {
            if (!fishEntity.Equals(fish) && fishAttributes.ValueRO.SchoolIndex == schoolIndex
                                         && math.distance(fishEntityTransform,
                                             new float3(fishTransform.ValueRO.Position.x,
                                                 fishTransform.ValueRO.Position.y,
                                                 fishTransform.ValueRO.Position.z)) < seperationRadius)
            {
                float3 difference = fishEntityTransform - fishTransform.ValueRO.Position;
                moveAway -= difference;
                seperationCount++;
            }
        }
        if (seperationCount > 0)
        {
            moveAway /= seperationCount;
        }
        return !moveAway.Equals(float3.zero) ? math.normalize(moveAway) : float3.zero;
    }
    
    // Rule 3
    [BurstCompile]
    public float3 Alignment(ref SystemState state, Entity fishEntity,
        float3 fishEntityTransform, int schoolIndex)
    {
        float3 averageVelocity = float3.zero;
        int count = 0;
        
        foreach (var (fishTransform, fishAttributes ,fish) in SystemAPI.Query<RefRW<LocalTransform>, RefRO<FishAttributes>>()
                     .WithEntityAccess())
        {
            if (fishAttributes.ValueRO.SchoolIndex == schoolIndex)
            {
                averageVelocity += fishAttributes.ValueRO.Velocity;
                count++;
            }
        }

        if (count > 0)
        {
            averageVelocity /= count;
            return math.normalize(averageVelocity);
        }
        return float3.zero;
    }
    
    [BurstCompile]
    public float3 MoveAwayFromWall(ref SystemState state, float3 posToMoveAwayFrom ,int schoolIndex, int multiplier)
    {
        float3 moveAwayFloaty = float3.zero;
        foreach (var (fishTransform, fishAttributes, fish) in SystemAPI
                     .Query<RefRO<LocalTransform>, RefRO<FishAttributes>>()
                     .WithEntityAccess())
        {
            if (fishAttributes.ValueRO.SchoolIndex == schoolIndex)
            {
                moveAwayFloaty += -multiplier * fishAttributes.ValueRO.PosToMoveAwayFrom -
                                  fishTransform.ValueRO.Position;
            }
        }
        return moveAwayFloaty;
    }
    
    
    [BurstCompile]
    // should be similar to vector3 clampMagnitude
    public float3 ClampLength(float3 floaty, float maxLength)
    {
            if (math.lengthsq(floaty) > maxLength * maxLength)
            {
                float scale = maxLength / math.length(floaty);
                return floaty * scale;
            }
            return floaty;
    }
}



