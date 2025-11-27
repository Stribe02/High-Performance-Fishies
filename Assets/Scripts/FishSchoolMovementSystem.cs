using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

partial struct FishSchoolMovementSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton
        >();
        //state.RequireForUpdate<FishAttributes>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb =
            SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton
            >().CreateCommandBuffer(state.WorldUnmanaged);

        // should loop through the different school the cohesion etc. methods needs to loop through the fish of the school they get
        foreach (var (fishSchoolAttribute, fishSchool) in SystemAPI.Query<RefRW<FishSchoolAttribute>>()
                     .WithEntityAccess())
        {
            foreach (var fish in fishSchoolAttribute.ValueRW.Fishes)
            {
                Vector3 fishPosition = state.EntityManager.GetComponentData<LocalTransform>(fish).Position;
                Vector3 cohesion = Cohesion(ref state, fish, fishSchoolAttribute.ValueRW.Fishes ,fishPosition, fishSchoolAttribute.ValueRO.SchoolIndex) *
                                   fishSchoolAttribute.ValueRO.CohesionWeight;
                Vector3 separation =
                    Separation(ref state, fish,fishSchoolAttribute.ValueRW.Fishes ,fishPosition, fishSchoolAttribute.ValueRO.SchoolIndex,
                        fishSchoolAttribute.ValueRO.SeparationRadius) * fishSchoolAttribute.ValueRO.SeparationWeight;
                Vector3 alignment = Alignment(ref state, fish,fishSchoolAttribute.ValueRW.Fishes ,fishPosition, fishSchoolAttribute.ValueRO.SchoolIndex) *
                                    fishSchoolAttribute.ValueRO.AlignmentWeight;



                var fishRotation = state.EntityManager.GetComponentData<LocalTransform>(fish).Rotation;
                var fishData = state.EntityManager.GetComponentData<FishAttributes>(fish);
                var aquaData = state.EntityManager.GetComponentData<AquaticAnimalAttributes>(fish);
                fishData.Velocity += cohesion + separation + alignment;
                fishData.Velocity = Vector3.ClampMagnitude(fishData.Velocity,
                    aquaData.Speed);
                fishPosition += fishData.Velocity * SystemAPI.Time.DeltaTime;
                fishRotation = UnityEngine.Quaternion.LookRotation(fishData.Velocity);
                ecb.SetComponent<FishAttributes>(fish, new FishAttributes
                {
                    Velocity = fishData.Velocity,
                    SchoolIndex = fishData.SchoolIndex
                });
                ecb.SetComponent<LocalTransform>(fish, new LocalTransform
                {
                    Position = fishPosition,
                    Rotation = fishRotation,
                    Scale = state.EntityManager.GetComponentData<LocalTransform>(fish).Scale
                });
            }
        }
    }

    // Rule 1:
    [BurstCompile]
    public Vector3 Cohesion(ref SystemState state, Entity fishEntity,  NativeArray<Entity> schoolFishes, Vector3 fishEntityTransform, int schoolIndex)
    {
        // Rule 1: Cohesion
        Vector3 centerOfMass = Vector3.zero; // need to be float3???
        int cohesionCount = 0;
        
        foreach (var (fishTransform, fishAttributes ,fish) in SystemAPI.Query<RefRW<LocalTransform>, RefRO<FishAttributes>>()
                     .WithEntityAccess())
        {
            if (fishAttributes.ValueRO.SchoolIndex == schoolIndex && !fishEntity.Equals(fish))
            {
                var pos = new Vector3(fishTransform.ValueRO.Position.x, fishTransform.ValueRO.Position.y,
                    fishTransform.ValueRO.Position.z);
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

    // Rule 2:
    [BurstCompile]
    public Vector3 Separation(ref SystemState state, Entity fishEntity, NativeArray<Entity> schoolFishes,
        Vector3 fishEntityTransform, int schoolIndex, float seperationRadius)
    {
        Vector3 moveAway = Vector3.zero;
        int seperationCount = 0;
        
        
        foreach (var (fishTransform, fishAttributes ,fish) in SystemAPI.Query<RefRW<LocalTransform>, RefRO<FishAttributes>>()
                     .WithEntityAccess())
        {
            if (!fishEntity.Equals(fish) && fishAttributes.ValueRO.SchoolIndex == schoolIndex
                                         && Vector3.Distance(fishEntityTransform,
                                             new Vector3(fishTransform.ValueRO.Position.x,
                                                 fishTransform.ValueRO.Position.y,
                                                 fishTransform.ValueRO.Position.z)) < seperationRadius)
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

    
    // Rule 3
    [BurstCompile]
    public Vector3 Alignment(ref SystemState state, Entity fishEntity, NativeArray<Entity> fishSchool,
        Vector3 fishEntityTransform, int schoolIndex)
    {
        Vector3 averageVelocity = Vector3.zero;
        int count = 0;
        
        foreach (var (fishTransform, fishAttributes ,fish) in SystemAPI.Query<RefRW<LocalTransform>, RefRO<FishAttributes>>()
                     .WithEntityAccess())
        {
            if (fishAttributes.ValueRO.SchoolIndex == schoolIndex)
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
}
