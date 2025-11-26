using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
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

        //state.RequireForUpdate<FishAttributes>();

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
                Vector3 fishPosition = state.EntityManager.GetComponentData<LocalTransform>(fish.Fish).Position;
                Vector3 cohesion = Cohesion(ref state, fish.Fish, schoolBuffer, fishPosition, fishSchoolAttribute.ValueRO.SchoolIndex) *
                                   fishSchoolAttribute.ValueRO.CohesionWeight;
                Vector3 separation =
                    Separation(ref state, fish.Fish, schoolBuffer, fishPosition, fishSchoolAttribute.ValueRO.SchoolIndex,
                        fishSchoolAttribute.ValueRO.SeparationRadius) * fishSchoolAttribute.ValueRO.SeparationWeight;
                Vector3 alignment = Alignment(ref state, fish.Fish, schoolBuffer, fishPosition, fishSchoolAttribute.ValueRO.SchoolIndex) *
                                    fishSchoolAttribute.ValueRO.AlignmentWeight;



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
        Vector3 centerOfMass = Vector3.zero; // need to be float3???
        int cohesionCount = 0;
        foreach (var fish in schoolFishes)
        {
            var fishData = state.EntityManager.GetComponentData<FishAttributes>(fish.Fish);
            var fishTransform = state.EntityManager.GetComponentData<LocalTransform>(fish.Fish);
                    
            if (!fish.Fish.Equals(fishEntity) && fishData.SchoolIndex == schoolIndex)
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
    }

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

    }

    
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
}
