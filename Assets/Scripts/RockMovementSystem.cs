using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;


partial struct RockMovementSystem : ISystem
{


    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<SimulationSingleton>();
        state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();

    }

    public void OnUpdate(ref SystemState state)
    {
        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        foreach (var (rockTransform, rockComponent, entity) in
                 SystemAPI.Query<RefRW<LocalTransform>, RefRW<RockComponent>>()
                     .WithAll<RockComponent>() // exclude the player tank from the query
                     .WithEntityAccess())
        {
            rockTransform.ValueRW.Position += rockComponent.ValueRO.Velocity * SystemAPI.Time.DeltaTime;
        }
    }
}
