using System.Numerics;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Vector3 = UnityEngine.Vector3;
using UnityEngine;


partial struct FishBoidsSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<FishAttributes>();
        state.RequireForUpdate<AquaticAnimalAttributes>();
    }
    
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (transform, fishAttributes, aquaticAnimalAttri ,entity) in
                 SystemAPI.Query<RefRW<LocalTransform>, RefRW<FishAttributes>, RefRW<AquaticAnimalAttributes>>()
                     .WithEntityAccess())
        {
            
            Vector3 cohesion = Cohesion(ref state, entity, transform.ValueRW.Position);
            Vector3 separation = Separation(ref state, entity, transform.ValueRW.Position, 2f) * 1f;
            Vector3 alignment = Alignment(ref state, entity, transform.ValueRW.Position) * 1f;
            fishAttributes.ValueRW.Velocity += cohesion + separation + alignment;
            Debug.Log("speed: " + aquaticAnimalAttri.ValueRW.Speed);
            fishAttributes.ValueRW.Velocity = Vector3.ClampMagnitude(fishAttributes.ValueRW.Velocity, aquaticAnimalAttri.ValueRW.Speed);
            float3 velocity = fishAttributes.ValueRW.Velocity;
            transform.ValueRW.Position += velocity * SystemAPI.Time.DeltaTime;
            transform.ValueRW.Rotation = UnityEngine.Quaternion.LookRotation(fishAttributes.ValueRW.Velocity);
        }
    }

    // Rule 1:
    public Vector3 Cohesion(ref SystemState state, Entity fishEntity, Vector3 fishEntityTransform)
    {
        // Rule 1: Cohesion
        Vector3 centerOfMass = Vector3.zero; // need to be float3???
        int cohesionCount = 0;
        foreach (var (transform, entity) in
                 SystemAPI.Query<RefRW<LocalTransform>>()
                     .WithAll<FishAttributes>() // Get all with fish tag
                     .WithEntityAccess())
        {
            if (!entity.Equals(fishEntity))
            {
                var pos = new Vector3(transform.ValueRO.Position.x, transform.ValueRO.Position.y, transform.ValueRO.Position.z);
                centerOfMass += pos;
                cohesionCount++;
            }
        }
       
        if (cohesionCount > 0)
        {
            centerOfMass /= cohesionCount;
            return centerOfMass - fishEntityTransform / 100; // maybe this works?
        }

        return float3.zero;
    }
    
    // Rule 2:
    public Vector3 Separation(ref SystemState state, Entity fishEntity, Vector3 fishEntityTransform, float seperationRadius)
    {
        Vector3 moveAway = Vector3.zero;
        int seperationCount = 0;
        foreach (var (transform, entity) in
                 SystemAPI.Query<RefRW<LocalTransform>>()
                     .WithAll<FishTag>() // Get all with fish tag
                     .WithEntityAccess())
        {
            if (!entity.Equals(fishEntity) &&
                Vector3.Distance(fishEntityTransform, new Vector3(transform.ValueRW.Position.x, transform.ValueRW.Position.y, transform.ValueRW.Position.z)) < seperationRadius)
            {
                Vector3 difference = fishEntityTransform - new Vector3(transform.ValueRW.Position.x, transform.ValueRW.Position.y, transform.ValueRW.Position.z);
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
        public Vector3 Alignment(ref SystemState state,Entity fishEntity, Vector3 fishEntityTransform)
        {
            Vector3 averageVelocity = Vector3.zero;
            int count = 0;
            foreach (var (transform, entity) in
                     SystemAPI.Query<RefRW<LocalTransform>>()
                         .WithAll<FishAttributes>() // Get all with fish tag
                         .WithEntityAccess())
            {
                if (!entity.Equals(fishEntity))
                {
                    averageVelocity += new Vector3(transform.ValueRW.Position.x, transform.ValueRW.Position.y, transform.ValueRW.Position.z);
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
