using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

partial struct FishSchoolMovementSystem : ISystem
{

    BufferLookup<SchoolFishes> schoolFishesLookup;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton
        >();

        schoolFishesLookup = state.GetBufferLookup<SchoolFishes>();

    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        schoolFishesLookup.Update(ref state);
        var ecb =
            SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton
            >().CreateCommandBuffer(state.WorldUnmanaged);

        // should loop through the different school the cohesion etc. methods needs to loop through the fish of the school they get
        foreach (var (fishSchoolAttribute, fishBuffer, fishSchool) in SystemAPI.Query<RefRW<FishSchoolAttribute>, DynamicBuffer<SchoolFishes>>().WithEntityAccess())
        {
            var schoolBuffer = state.EntityManager.GetBuffer<SchoolFishes>(fishSchool);

            foreach (var fish in schoolBuffer)
            {
                Vector3 fishPosition = state.EntityManager.GetComponentData<LocalTransform>(fish).Position;
            /*
            *                Vector3 fishPosition = state.EntityManager.GetComponentData<LocalTransform>(fish.Fish).Position;
                Vector3 cohesion = Cohesion(ref state, fish.Fish, schoolBuffer, fishPosition, fishSchoolAttribute.ValueRO.SchoolIndex) *
                                   fishSchoolAttribute.ValueRO.CohesionWeight;
                Vector3 separation =
                    Separation(ref state, fish.Fish, schoolBuffer, fishPosition, fishSchoolAttribute.ValueRO.SchoolIndex,
                        fishSchoolAttribute.ValueRO.SeparationRadius) * fishSchoolAttribute.ValueRO.SeparationWeight;
                Vector3 alignment = Alignment(ref state, fish.Fish, schoolBuffer, fishPosition, fishSchoolAttribute.ValueRO.SchoolIndex) *
                                    fishSchoolAttribute.ValueRO.AlignmentWeight;
            */

            /* Cohesion Job */
            NativeArray<Vector3> centerOfMass = new NativeArray<Vector3>(1, Allocator.TempJob);
            NativeArray<int> cohesionCount = new NativeArray<int>(1, Allocator.TempJob);
            CohesionJob cohesionJob = new CohesionJob
            {
                fishEntity = fish,
                centerOfMass = centerOfMass,
                cohesionCount = cohesionCount,
                schoolIndex = fishSchoolAttribute.ValueRO.SchoolIndex
            };
            var cohesionHandle = cohesionJob.Schedule(state.Dependency);
            
            
            /* Seperation Job */
            NativeArray<Vector3> moveAway = new NativeArray<Vector3>(1, Allocator.TempJob);
            NativeArray<int> seperationCount = new NativeArray<int>(1, Allocator.TempJob);


            SeparationJob separationJob = new SeparationJob
            {
                fishEntity = fish,
                fishEntityTransform = fishPosition,
                moveAway = moveAway,
                seperationCount = seperationCount,
                schoolIndex = fishSchoolAttribute.ValueRO.SchoolIndex,
                seperationRadius = fishSchoolAttribute.ValueRO.SeparationRadius
            };
            var separationHandle = separationJob.Schedule(state.Dependency);
            
            /* AlignmentJob: */

                NativeArray<Vector3> averageVelocity = new NativeArray<Vector3>(1, Allocator.TempJob);
                NativeArray<int> alignmentCount = new NativeArray<int>(1, Allocator.TempJob);
                AlignmentJob alignmentJob = new AlignmentJob
                {
                    averageVelocity = averageVelocity,
                    count = alignmentCount,
                    schoolIndex = fishSchoolAttribute.ValueRO.SchoolIndex
                };
                
                JobHandle jobHandle = alignmentJob.Schedule(state.Dependency);
                // make sure the jobs are completed
               
                // yoink the values from job
                cohesionHandle.Complete();
                var cohesion = ((centerOfMass[0] / cohesionCount[0]) - fishPosition).normalized * fishSchoolAttribute.ValueRO.CohesionWeight;
                
                separationHandle.Complete();
                var separation = (moveAway[0] /= seperationCount[0]).normalized * fishSchoolAttribute.ValueRO.SeparationWeight;
                
                jobHandle.Complete();
                var alignment = (averageVelocity[0] /= alignmentCount[0]-1).normalized * fishSchoolAttribute.ValueRO.AlignmentWeight;

                
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

                fishData.Velocity = Vector3.ClampMagnitude(fishData.Velocity,
                    aquaData.Speed);
                fishPosition += fishData.Velocity * SystemAPI.Time.DeltaTime;
                fishRotation = UnityEngine.Quaternion.LookRotation(fishData.Velocity);
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

    // Rule 1:
    [BurstCompile]
    public Vector3 Cohesion(ref SystemState state, Entity fishEntity, DynamicBuffer<SchoolFishes> schoolFishes, Vector3 fishEntityTransform, int schoolIndex)
    {
        // Rule 1: Cohesion
        Vector3 centerOfMass = Vector3.zero;
        int cohesionCount = 0;

        foreach (var (fishAttributes, localTransform, fish) in SystemAPI
                     .Query<RefRW<FishAttributes>, RefRW<LocalTransform>>()
                     .WithEntityAccess())
        {
            var fishData = state.EntityManager.GetComponentData<FishAttributes>(fish.Fish);
            var fishTransform = state.EntityManager.GetComponentData<LocalTransform>(fish.Fish);
                    
            if (!fish.Fish.Equals(fishEntity) && fishData.SchoolIndex == schoolIndex)
            {
                var pos = new Vector3(localTransform.ValueRO.Position.x, localTransform.ValueRO.Position.y,
                    localTransform.ValueRO.Position.z);
                centerOfMass += pos;
                cohesionCount++;
            }
        }

        if (cohesionCount > 0)
        {
            centerOfMass /= cohesionCount;
            return (centerOfMass - fishEntityTransform).normalized;
        }

        return Vector3.zero;
    }
    [BurstCompile]
    public partial struct CohesionJob : IJobEntity
    {
        public NativeArray<Vector3> centerOfMass;
        public int schoolIndex;
        public Entity fishEntity;
        public NativeArray<int> cohesionCount;

        public void Execute(in LocalTransform fishTransform, in FishAttributes fishAttributes, in Entity fish)
        {
            if (fishAttributes.SchoolIndex == schoolIndex && !fishEntity.Equals(fish))
            {
                var pos = new Vector3(fishTransform.Position.x, fishTransform.Position.y,
                    fishTransform.Position.z);
                centerOfMass[0] += pos;
                cohesionCount[0]++;
            }
        }
    }
    // basically saying for each fish in the school of fishes
    /*
    foreach (var fish in schoolFishes)
    {
        var fishData = state.EntityManager.GetComponentData<FishAttributes>(fish);
        var fishTransform = state.EntityManager.GetComponentData<LocalTransform>(fish);

        if (!fish.Equals(fishEntity) && fishData.SchoolIndex == schoolIndex)
        {
            var pos = new Vector3(fishTransform.Position.x, fishTransform.Position.y,
                fishTransform.Position.z);
            centerOfMass += pos;
            cohesionCount++;
        }
    }
    if (cohesionCount > 0)
    {
        centerOfMass /= cohesionCount;
        return (centerOfMass - fishEntityTransform).normalized;
    }

    return Vector3.zero;
}*/

    // Rule 2:
    [BurstCompile]
    public Vector3 Separation(ref SystemState state, Entity fishEntity, DynamicBuffer<SchoolFishes> schoolFishes,
        Vector3 fishEntityTransform, int schoolIndex, float seperationRadius)
    {
        Vector3 moveAway = Vector3.zero;
        int seperationCount = 0;
        foreach (var neighbor in schoolFishes)
        {
            var fishData = state.EntityManager.GetComponentData<FishAttributes>(neighbor.Fish);
            var fishTransform = state.EntityManager.GetComponentData<LocalTransform>(neighbor.Fish);

            if (!neighbor.Fish.Equals(fishEntity) && fishData.SchoolIndex == schoolIndex &&
                Vector3.Distance(fishEntityTransform,
                    new Vector3(fishTransform.Position.x, fishTransform.Position.y,
                        fishTransform.Position.z)) < seperationRadius)
            {
                Vector3 difference = fishEntityTransform - new Vector3(fishTransform.ValueRO.Position.x,
                    fishTransform.ValueRO.Position.y, fishTransform.ValueRO.Position.z);
                moveAway += difference.normalized / difference.magnitude; // something with scaling
                seperationCount++;
            }
        }

        if (seperationCount > 0)
        {
            moveAway /= seperationCount;
        }

        return moveAway.normalized;
    }
    [BurstCompile]
    public partial struct SeparationJob : IJobEntity
    {
        public NativeArray<Vector3> moveAway;
        public int schoolIndex;
        public Entity fishEntity;
        public Vector3 fishEntityTransform;
        public float seperationRadius;
        public NativeArray<int> seperationCount;

        public void Execute(in LocalTransform fishTransform, in FishAttributes fishAttributes, in Entity fish)
        {
            if (!fishEntity.Equals(fish) && fishAttributes.SchoolIndex == schoolIndex
                                         && Vector3.Distance(fishEntityTransform,
                                             new Vector3(fishTransform.Position.x,
                                                 fishTransform.Position.y,
                                                 fishTransform.Position.z)) < seperationRadius)
            {
                Vector3 difference = fishEntityTransform - new Vector3(fishTransform.Position.x,
                    fishTransform.Position.y, fishTransform.Position.z);
                moveAway[0] += difference.normalized / difference.magnitude; // something with scaling
                seperationCount[0]++;
            }
        }
    }

    /*
    foreach (var neighbor in schoolFishes)
    {
        var fishData = state.EntityManager.GetComponentData<FishAttributes>(neighbor);
        var fishTransform = state.EntityManager.GetComponentData<LocalTransform>(neighbor);

        if (!neighbor.Equals(fishEntity) && fishData.SchoolIndex == schoolIndex &&
            Vector3.Distance(fishEntityTransform,
                new Vector3(fishTransform.Position.x, fishTransform.Position.y,
                    fishTransform.Position.z)) < seperationRadius)
        {
            Vector3 difference = fishEntityTransform - new Vector3(fishTransform.Position.x,
                fishTransform.Position.y, fishTransform.Position.z);
            moveAway += difference.normalized / difference.magnitude; // something with scaling
            seperationCount++;
        }
    }

    if (seperationCount > 0)
    {
        moveAway /= seperationCount;
    }

    return moveAway.normalized;

}*/


    // Rule 3
    [BurstCompile]
    public Vector3 Alignment(ref SystemState state, Entity fishEntity, DynamicBuffer<SchoolFishes> fishSchool,
        Vector3 fishEntityTransform, int schoolIndex)
    {
        Vector3 averageVelocity = Vector3.zero;
        int count = 0;
        foreach (var fish in fishSchool)
        {
            var fishData = state.EntityManager.GetComponentData<FishAttributes>(fish.Fish);
            var fishTransform = state.EntityManager.GetComponentData<LocalTransform>(fish.Fish);

            if (!fish.Fish.Equals(fishEntity) && fishData.SchoolIndex == schoolIndex)
            {
                averageVelocity += new Vector3(fishTransform.ValueRO.Position.x, fishTransform.ValueRO.Position.y,
                    fishTransform.ValueRO.Position.z);
                count++;
            }
        }

        if (count > 0)
        {
            averageVelocity /= count;
            return averageVelocity.normalized;
        }
        
        return Vector3.zero;
    }
    
    [BurstCompile]
    public partial struct AlignmentJob : IJobEntity
    {
        public NativeArray<Vector3> averageVelocity;
        public int schoolIndex;
        public NativeArray<int> count;

        public void Execute(in LocalTransform fishTransform, in FishAttributes fishAttributes, in Entity fish)
        {
            if (fishAttributes.SchoolIndex == schoolIndex)
            {
                averageVelocity[0] += new Vector3(fishTransform.Position.x, fishTransform.Position.y,
                    fishTransform.Position.z);
                count[0]++;
            }
        }
    }
}

/*
foreach (var fish in fishSchool)
{
    var fishData = state.EntityManager.GetComponentData<FishAttributes>(fish);
    var fishTransform = state.EntityManager.GetComponentData<LocalTransform>(fish);

    if (!fish.Equals(fishEntity) && fishData.SchoolIndex == schoolIndex)
    {
        averageVelocity += new Vector3(fishTransform.Position.x, fishTransform.Position.y,
            fishTransform.Position.z);
        count++;
    }
}

if (count > 0)
{
    averageVelocity /= count;
    return averageVelocity.normalized;
}

return Vector3.zero;
}
*/



