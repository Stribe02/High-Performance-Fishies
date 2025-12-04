using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[CreateAfter(typeof(FishSchoolSpawner))]
partial struct FishSchoolMovementSystem : ISystem
{

    BufferLookup<SchoolFishes> schoolFishesLookup;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton
        >();
        state.RequireForUpdate<Config>();
        schoolFishesLookup = state.GetBufferLookup<SchoolFishes>();
        
    }

   
    public void OnUpdate(ref SystemState state)
    {
        schoolFishesLookup.Update(ref state);
        var ecb =
            SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton
            >().CreateCommandBuffer(state.WorldUnmanaged);
        
        var config = SystemAPI.GetSingleton<Config>();


        // should loop through the different school the cohesion etc. methods needs to loop through the fish of the school they get
        foreach (var (fishSchoolAttribute, fishBuffer, fishSchool) in SystemAPI.Query<RefRW<FishSchoolAttribute>, DynamicBuffer<SchoolFishes>>().WithEntityAccess())
        {
            var schoolBuffer = state.EntityManager.GetBuffer<SchoolFishes>(fishSchool);

            foreach (var fish in schoolBuffer)
            {
                float3 fishPosition = state.EntityManager.GetComponentData<LocalTransform>(fish.Fish).Position;

                
            /* Cohesion Job */
            NativeArray<float3> centerOfMass = new NativeArray<float3>(1, Allocator.TempJob);
            NativeArray<int> cohesionCount = new NativeArray<int>(1, Allocator.TempJob);

            CohesionJob cohesionJob = new CohesionJob
            {
                fishEntity = fish.Fish,
                centerOfMass = centerOfMass,
                cohesionCount = cohesionCount,
                schoolIndex = fishSchoolAttribute.ValueRO.SchoolIndex
            };

            JobHandle cohesionHandle = default;
            switch (config.ScheduleType)
            {
                case ScheduleType.Schedule:
                    cohesionHandle = cohesionJob.Schedule(state.Dependency);
                    break;
                case ScheduleType.ScheduleParallel:
                    cohesionHandle = cohesionJob.ScheduleParallel(state.Dependency);
                    break;
                case ScheduleType.Run:
                    //cohesionHandle = cohesionJob.Run();
                    break;
                default:
                    break;
            }
            
            /* Seperation Job */
            NativeArray<float3> moveAway = new NativeArray<float3>(1, Allocator.TempJob);
            NativeArray<int> seperationCount = new NativeArray<int>(1, Allocator.TempJob);


            SeparationJob separationJob = new SeparationJob
            {
                fishEntity = fish.Fish,
                fishEntityTransform = fishPosition,
                moveAway = moveAway,
                seperationCount = seperationCount,
                schoolIndex = fishSchoolAttribute.ValueRO.SchoolIndex,
                seperationRadius = fishSchoolAttribute.ValueRO.SeparationRadius
            };
            
            JobHandle separationHandle = default;
            switch (config.ScheduleType)
            {
                case ScheduleType.Schedule:
                    separationHandle = separationJob.Schedule(state.Dependency);
                    break;
                case ScheduleType.ScheduleParallel:
                    separationHandle = separationJob.ScheduleParallel(state.Dependency);
                    break;
                case ScheduleType.Run:
                    //separationHandle = separationJob.Run();
                    break;
                default:
                    break;
            }
            
            /* AlignmentJob: */

                NativeArray<float3> averageVelocity = new NativeArray<float3>(1, Allocator.TempJob);
                NativeArray<int> alignmentCount = new NativeArray<int>(1, Allocator.TempJob);
                AlignmentJob alignmentJob = new AlignmentJob
                {
                    averageVelocity = averageVelocity,
                    count = alignmentCount,
                    schoolIndex = fishSchoolAttribute.ValueRO.SchoolIndex
                };

                JobHandle alignmentHandle = default;
                switch (config.ScheduleType)
                {
                    case ScheduleType.Schedule:
                        alignmentHandle = alignmentJob.Schedule(state.Dependency);
                        break;
                    case ScheduleType.ScheduleParallel:
                        alignmentHandle = alignmentJob.ScheduleParallel(state.Dependency);
                        break;
                    case ScheduleType.Run:
                        //alignmentHandle = alignmentJob.Run();
                        break;
                    default:
                        break;
                }
                
                // make sure the jobs are completed
                // yoink the values from job
                cohesionHandle.Complete();
                alignmentHandle.Complete();
                separationHandle.Complete();
                if (seperationCount[0] > 0)
                {
                    moveAway[0] /= seperationCount[0];
                }
                
                float3 cohesion = math.normalize((centerOfMass[0] / cohesionCount[0]) - fishPosition) * fishSchoolAttribute.ValueRO.CohesionWeight;

                float3 separation = seperationCount[0] > 0 ?  math.normalize(moveAway[0] / seperationCount[0]) * fishSchoolAttribute.ValueRO.SeparationWeight : float3.zero;
                
                float3 alignment = math.normalize(averageVelocity[0] /= alignmentCount[0]-1) * fishSchoolAttribute.ValueRO.AlignmentWeight;

                
                // Dispose of the Arrays used in jobs:
                centerOfMass.Dispose();
                cohesionCount.Dispose();
                moveAway.Dispose();
                seperationCount.Dispose();
                averageVelocity.Dispose();
                alignmentCount.Dispose();
                
                /* Get the fish data and apply the rules */
                var fishRotation = state.EntityManager.GetComponentData<LocalTransform>(fish.Fish).Rotation;
                var fishData = state.EntityManager.GetComponentData<FishAttributes>(fish.Fish);
                var aquaData = state.EntityManager.GetComponentData<AquaticAnimalAttributes>(fish.Fish);
                fishData.Velocity += cohesion + separation + alignment;
                
                fishData.Velocity = clampLength(fishData.Velocity,
                    aquaData.Speed);
                
                
                
            
                
                fishPosition += fishData.Velocity * SystemAPI.Time.DeltaTime;
                fishRotation = quaternion.LookRotation(fishData.Velocity, new float3(0,1,0)); // look up?
                ecb.SetComponent<FishAttributes>(fish.Fish, new FishAttributes
                {
                    Velocity = fishData.Velocity,
                    SchoolIndex = fishData.SchoolIndex
                });
                ecb.SetComponent<LocalTransform>(fish.Fish, new LocalTransform
                {
                    Position = fishPosition,
                    Rotation = fishRotation,
                    Scale = state.EntityManager.GetComponentData<LocalTransform>(fish.Fish).Scale
                });
            }
        }
    }
    
    
    [BurstCompile]
    public partial struct CohesionJob : IJobEntity
    {
        [NativeDisableParallelForRestriction]
        public NativeArray<float3> centerOfMass;
        public int schoolIndex;
        public Entity fishEntity;
        [NativeDisableParallelForRestriction]
        public NativeArray<int> cohesionCount;

        public void Execute(in LocalTransform fishTransform, in FishAttributes fishAttributes, in Entity fish)
        {
            if (fishAttributes.SchoolIndex == schoolIndex && !fishEntity.Equals(fish))
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
        public Entity fishEntity;
        public float3 fishEntityTransform;
        public float seperationRadius;
        [NativeDisableParallelForRestriction]
        public NativeArray<int> seperationCount;

        public void Execute(in LocalTransform fishTransform, in FishAttributes fishAttributes, in Entity fish)
        {
            if (!fishEntity.Equals(fish) && fishAttributes.SchoolIndex == schoolIndex
                                         && (math.distance(fishEntityTransform,
                                             new float3(fishTransform.Position.x,
                                                 fishTransform.Position.y,
                                                 fishTransform.Position.z)) < seperationRadius))
            {
                float3 difference = fishEntityTransform - new float3(fishTransform.Position.x,
                    fishTransform.Position.y, fishTransform.Position.z);
                moveAway[0] += math.normalize(difference) / math.length(difference); // something with scaling
                seperationCount[0]++;
            }
        }
    }
    
    [BurstCompile]
    public partial struct AlignmentJob : IJobEntity
    {
        [NativeDisableParallelForRestriction]
        public NativeArray<float3> averageVelocity;
        public int schoolIndex;
        [NativeDisableParallelForRestriction]
        public NativeArray<int> count;

        public void Execute(in LocalTransform fishTransform, in FishAttributes fishAttributes, in Entity fish)
        {
            if (fishAttributes.SchoolIndex == schoolIndex)
            {
                averageVelocity[0] += new float3(fishTransform.Position.x, fishTransform.Position.y,
                    fishTransform.Position.z);
                count[0]++;
            }
        }
    }
    // should be similar to vector3 clampMagnitude
    public float3 clampLength(float3 floaty, float maxLength)
    {
        if (math.lengthsq(floaty) > maxLength * maxLength)
        {
            return math.normalize(floaty) * maxLength;
        }

        return floaty;
    }
}



