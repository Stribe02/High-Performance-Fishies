using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;

[UpdateBefore(typeof(PhysicsSimulationGroup))]
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
partial struct RockSpawnSystem : ISystem
{
    public float time;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<Config>();
        state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
        state.RequireForUpdate<RockSpawning>();
    }

    public void OnUpdate(ref SystemState state)
    {
        time += SystemAPI.Time.DeltaTime;
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
         
            
            var ran = new Unity.Mathematics.Random((uint)time + 1);
            float x = ran.NextFloat(-14.5f, 17f);
            float y = ran.NextFloat(0, 13.5f);
            float z = ran.NextFloat(0, 77);
            state.EntityManager.SetComponentData(rockEntity, new LocalTransform
            {
                Position = new float3(x, y, z),
                Rotation = rockTransform.Rotation,
                Scale = rockTransform.Scale
            });
            float3 velocity = new float3(ran.NextFloat(0, 20),ran.NextFloat(0, 2), ran.NextFloat(0,20));
            
            if (z > 60f)
            {
                velocity = new float3(velocity.x, velocity.y, -velocity.z);
            }

            state.EntityManager.SetComponentData(rockEntity, new RockComponent()
            {
                Velocity = velocity
            });
        }
    }
}
