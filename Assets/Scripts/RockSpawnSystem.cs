using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

partial struct RockSpawnSystem : ISystem
{

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<Config>();
        state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
        state.RequireForUpdate<RockSpawning>();
    }

    public void OnUpdate(ref SystemState state)
    {
        var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);
        var config = SystemAPI.GetSingleton<Config>();
        var rockSpawning = SystemAPI.GetSingleton<RockSpawning>();
        // if a bool is true: throw a rock!
        if (rockSpawning.ShouldSpawnRock)
        {

            var configEntity = state.EntityManager.CreateEntityQuery(typeof(Config)).GetSingletonEntity();
            state.EntityManager.SetComponentData<RockSpawning>(configEntity,
                new RockSpawning { ShouldSpawnRock = false });


            var rockTransform = state.EntityManager.GetComponentData<LocalTransform>(config.RockComponent);
            Entity rockEntity = state.EntityManager.Instantiate(config.RockComponent);

            rockTransform.Position = new float3(-10, 10, 10);

            state.EntityManager.SetComponentData(rockEntity, new RockComponent()
            {
                Velocity = new float3(2, 0, 14) // Could be random. y should be 0 because gravity on the rigid body is handling that
            });
        }
    }
}
