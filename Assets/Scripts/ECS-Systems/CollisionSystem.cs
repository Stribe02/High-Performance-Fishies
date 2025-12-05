using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;

//[UpdateBefore(typeof(PhysicsSimulationGroup))]
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
partial struct CollisionSystem : ISystem
{
    
    ComponentLookup<RockComponent> RockComponentLookup;
    ComponentLookup<WallTag> wallLookup;
    ComponentLookup<FishAttributes> fishLookup;
    ComponentLookup<LocalTransform> localTransformUp;
    
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<SimulationSingleton>();
        state.RequireForUpdate<PhysicsWorldSingleton>();
        state.RequireForUpdate<Config>();
        RockComponentLookup = state.GetComponentLookup<RockComponent>();
        wallLookup = state.GetComponentLookup<WallTag>(true);
        fishLookup = state.GetComponentLookup<FishAttributes>();
        localTransformUp = state.GetComponentLookup<LocalTransform>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        RockComponentLookup.Update(ref state);
        wallLookup.Update(ref state);
        fishLookup.Update(ref state);
        localTransformUp.Update(ref state);

        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);
        var physicsWorldSingleton = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
        var config = SystemAPI.GetSingleton<Config>();

        if (config.ScheduleType == ScheduleType.Schedule || config.ScheduleType == ScheduleType.ScheduleParallel)
        {
            state.Dependency = new CheckCollisionRockJob
            {
                ecb = ecb,
                rockComponent = RockComponentLookup,
                wall = wallLookup
            }.Schedule(SystemAPI.GetSingleton<SimulationSingleton>(), state.Dependency);

            state.Dependency = new CheckCollisionFishJob
            {
                ecb = ecb,
                fish = fishLookup,
                wall = wallLookup,
                PhysicsWorldSingleton = physicsWorldSingleton,
                localTransform = localTransformUp
            }.Schedule(SystemAPI.GetSingleton<SimulationSingleton>(), state.Dependency);
        }
        else
        {
            var sim = SystemAPI.GetSingleton<SimulationSingleton>().AsSimulation();
            // Wait for jobs to complete so we can access the collisionEvents
            sim.FinalJobHandle.Complete();

            CheckCollisionRock(ref state, ecb, sim.CollisionEvents);
            CheckCollisionFish(ref state, ecb, physicsWorldSingleton, sim.CollisionEvents);
        }
    }

    public void CheckCollisionRock(ref SystemState state, EntityCommandBuffer ecb ,CollisionEvents collisionEvents)
    {
        foreach (var collisionEvent in collisionEvents)
        {
            Entity rockEntity = Entity.Null;

            if (SystemAPI.HasComponent<RockComponent>(collisionEvent.EntityA) &&
                SystemAPI.HasComponent<WallTag>(collisionEvent.EntityB))
            {
                rockEntity = collisionEvent.EntityA;
            }
            else if (SystemAPI.HasComponent<RockComponent>(collisionEvent.EntityB) &&
                     SystemAPI.HasComponent<WallTag>(collisionEvent.EntityA))
            {
                rockEntity = collisionEvent.EntityB;
            }

            if (rockEntity.Equals(Entity.Null)) return;
            ecb.DestroyEntity(rockEntity);
        }
    }
    
    public void CheckCollisionFish(ref SystemState state, EntityCommandBuffer ecb, PhysicsWorldSingleton physicsWorldSingleton ,CollisionEvents collisionEvents)
    {
        foreach (var collisionEvent in collisionEvents)
        {
            Entity fishEntity = Entity.Null;

            if (SystemAPI.HasComponent<FishAttributes>(collisionEvent.EntityA) &&
                SystemAPI.HasComponent<WallTag>(collisionEvent.EntityB))
            {
                fishEntity = collisionEvent.EntityA;
            }
            else if (SystemAPI.HasComponent<RockComponent>(collisionEvent.EntityB) &&
                     SystemAPI.HasComponent<WallTag>(collisionEvent.EntityA))
            {
                fishEntity = collisionEvent.EntityB;
            }

            if (fishEntity.Equals(Entity.Null)) return;
            
            var collisionDetails = collisionEvent.CalculateDetails(ref physicsWorldSingleton.PhysicsWorld);
            var avgContactPointPosition = collisionDetails.AverageContactPointPosition;
            var fish = SystemAPI.GetComponentRW<FishAttributes>(fishEntity);
            fish.ValueRW.HasHitWall = true;
            fish.ValueRW.PosToMoveAwayFrom = avgContactPointPosition;

        }
    }



    [BurstCompile]
    public partial struct CheckCollisionRockJob : ICollisionEventsJob
    {
        public EntityCommandBuffer ecb;
        public ComponentLookup<RockComponent> rockComponent;
        [ReadOnly] public ComponentLookup<WallTag> wall;

        public void Execute(CollisionEvent collisionEvent)
        {
            Entity rockEntity;
            
            // one collision detect, rock can be either entity A or B, so check for both.
            // Is rock hitting a wall?
            if (rockComponent.HasComponent(collisionEvent.EntityA) && wall.HasComponent(collisionEvent.EntityB))
            {
                rockEntity = collisionEvent.EntityA;
            }
            else if (rockComponent.HasComponent(collisionEvent.EntityB) && wall.HasComponent(collisionEvent.EntityA))
            {
                rockEntity = collisionEvent.EntityB;
            }
            else rockEntity = Entity.Null;

            if (rockEntity.Equals(Entity.Null)) return;
            
            ecb.DestroyEntity(rockEntity);
        }
    }
    
    [BurstCompile]
    public partial struct CheckCollisionFishJob : ICollisionEventsJob
    {
        public EntityCommandBuffer ecb;
        // we need to look entities with fishes
        public ComponentLookup<FishAttributes> fish;
        [ReadOnly] public ComponentLookup<WallTag> wall;
        public ComponentLookup<LocalTransform> localTransform;
        [ReadOnly] public PhysicsWorldSingleton PhysicsWorldSingleton;

        public void Execute(CollisionEvent collisionEvent)
        {
            Entity fishEntity;
            Entity wallEntity; 
            
            // one collision detect, rock can be either entity A or B, so check for both.
            // Is rock hitting a wall?
            if (fish.HasComponent(collisionEvent.EntityA) && wall.HasComponent(collisionEvent.EntityB))
            {
                fishEntity = collisionEvent.EntityA;
                wallEntity = collisionEvent.EntityB;
            }
            else if (fish.HasComponent(collisionEvent.EntityB) && wall.HasComponent(collisionEvent.EntityA))
            {
                fishEntity = collisionEvent.EntityB;
                wallEntity = collisionEvent.EntityA;
            }
            else
            {
                fishEntity = Entity.Null;
                wallEntity = Entity.Null;
            }

            if (fishEntity.Equals(Entity.Null) && wallEntity.Equals(Entity.Null)) return;

            /*
             * If one fish bonks into a wall - find out what fish school it comes from
             * and adjust the entire school's movement instead.
             */
                var collisionDetails = collisionEvent.CalculateDetails(ref PhysicsWorldSingleton.PhysicsWorld);
                var avgContactPointPosition = collisionDetails.AverageContactPointPosition;
                fish.GetRefRW(fishEntity).ValueRW.HasHitWall = true;
                fish.GetRefRW(fishEntity).ValueRW.PosToMoveAwayFrom = avgContactPointPosition;
        }
    }
}
