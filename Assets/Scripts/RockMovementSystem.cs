using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;

[UpdateBefore(typeof(PhysicsSimulationGroup))]
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
partial struct RockMovementSystem : ISystem
{
    ComponentLookup<RockComponent> RockComponentLookup;
    ComponentLookup<WallTag> wallLookup;



    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<SimulationSingleton>();
        state.RequireForUpdate<PhysicsWorldSingleton>();
        RockComponentLookup = state.GetComponentLookup<RockComponent>();
        wallLookup = state.GetComponentLookup<WallTag>(true);

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

        RockComponentLookup.Update(ref state);
        wallLookup.Update(ref state);

        state.Dependency = new CountNumCollisionEvents
        {
            ecb = ecb,
            rockComponent = RockComponentLookup,
            wall = wallLookup
        }.Schedule(SystemAPI.GetSingleton<SimulationSingleton>(), state.Dependency);
    }



    public partial struct CountNumCollisionEvents : ICollisionEventsJob
    {
        public EntityCommandBuffer ecb;
        public ComponentLookup<RockComponent> rockComponent;
        [ReadOnly] public ComponentLookup<WallTag> wall;


        public void Execute(CollisionEvent collisionEvent)
        {
            Entity rockEntity;

            if (rockComponent.HasComponent(collisionEvent.EntityA) && wall.HasComponent(collisionEvent.EntityB))
            {
                rockEntity = collisionEvent.EntityA;
            }
            else if (rockComponent.HasComponent(collisionEvent.EntityB) && wall.HasComponent(collisionEvent.EntityA))
            {
                rockEntity = collisionEvent.EntityB;
            }
            else rockEntity = Entity.Null;

            if (!rockEntity.Equals(Entity.Null))
            {
                ecb.DestroyEntity(rockEntity);
            }
        }
    }
}
