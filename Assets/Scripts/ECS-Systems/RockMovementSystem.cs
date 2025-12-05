using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;


partial struct RockMovementSystem : ISystem
{

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<SimulationSingleton>();
        state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();

    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);
        var config = SystemAPI.GetSingleton<Config>();


        var rockMoveJob = new RockMoveJob
        {
            ecb = ecb.AsParallelWriter(),
            dt = Time.deltaTime
        };

        JobHandle rockMoveHandle = default;
        switch (config.ScheduleType) {
            case ScheduleType.Schedule:
                rockMoveHandle = rockMoveJob.Schedule(state.Dependency);
                break;
            case ScheduleType.ScheduleParallel:
                rockMoveHandle = rockMoveJob.ScheduleParallel(state.Dependency);
                break;
            case ScheduleType.Run:
                foreach (var (rockTransform, rockComponent, entity) in
                         SystemAPI.Query<RefRW<LocalTransform>, RefRW<RockComponent>>()
                             .WithAll<RockComponent>()
                             .WithEntityAccess())
                {
                    rockTransform.ValueRW.Position += rockComponent.ValueRO.Velocity * SystemAPI.Time.DeltaTime;
                }
                break;
            default:
                break;
        }
        rockMoveHandle.Complete();
    }

    [BurstCompile]
    public partial struct RockMoveJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ecb;
        public float dt;
        public void Execute(ref LocalTransform transform, in RockComponent rock)
        {
            transform.Position += rock.Velocity * dt;
        }
    }
}
