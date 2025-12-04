using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

partial struct CollisionSystem : ISystem
{
    //TODO ADD BURSTCOMPILE BACK
    ComponentLookup<RockComponent> RockComponentLookup;
    ComponentLookup<Wall> wallLookup;
    ComponentLookup<FishAttributes> fishLookup;
    ComponentLookup<LocalTransform> localTransformUp;
    
    
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<SimulationSingleton>();
        state.RequireForUpdate<PhysicsWorldSingleton>();
        RockComponentLookup = state.GetComponentLookup<RockComponent>();
        wallLookup = state.GetComponentLookup<Wall>(true);
        fishLookup = state.GetComponentLookup<FishAttributes>();
        localTransformUp = state.GetComponentLookup<LocalTransform>();
    }

    
    public void OnUpdate(ref SystemState state)
    {
        
        RockComponentLookup.Update(ref state);
        wallLookup.Update(ref state);
        fishLookup.Update(ref state);
        localTransformUp.Update(ref state);

        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);
        var physicsWorldSingleton = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
        
        state.Dependency = new CheckCollisionRock
        {
            ecb = ecb,
            rockComponent = RockComponentLookup,
            wall = wallLookup
        }.Schedule(SystemAPI.GetSingleton<SimulationSingleton>(), state.Dependency);
        
        state.Dependency = new CheckCollisionFish
        {
            ecb = ecb,
            fish = fishLookup,
            wall = wallLookup,
            PhysicsWorldSingleton = physicsWorldSingleton,
            localTransform = localTransformUp
        }.Schedule(SystemAPI.GetSingleton<SimulationSingleton>(), state.Dependency);
    }
    
    
    
    public partial struct CheckCollisionRock : ICollisionEventsJob
    {
        public EntityCommandBuffer ecb;
        public ComponentLookup<RockComponent> rockComponent;
        [ReadOnly] public ComponentLookup<Wall> wall;

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

            if (!rockEntity.Equals(Entity.Null))
            {
                ecb.DestroyEntity(rockEntity);
            }
        }
    }
    
    public partial struct CheckCollisionFish : ICollisionEventsJob
    {
        public EntityCommandBuffer ecb;
        // we need to look entities with fishes
        public ComponentLookup<FishAttributes> fish;
        [ReadOnly] public ComponentLookup<Wall> wall;
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

            if (!fishEntity.Equals(Entity.Null) && !wallEntity.Equals(Entity.Null))
            {
                // if fishes are colliding with wall:
                /*
                 * If fish hit ceiling: y coord is too high, change. Opposite for floor
                 * If fish hit right: x coord too high, Opposite for Left
                 * If fish hit Front: z coord too low, opposite for Back
                 */
                WallType wallType = wall.GetRefRO(wallEntity).ValueRO.WType;
                //float3 localPos = this.localTransform.GetRefRW(fishEntity).ValueRW.Position
                switch (wallType)
                {
                    case WallType.Ceiling:
                        ecb.SetComponent<LocalTransform>(fishEntity, new LocalTransform
                        {
                            Position = localTransform.GetRefRW(fishEntity).ValueRW.Position - new float3(0,-10,0),
                            Rotation = localTransform.GetRefRO(fishEntity).ValueRO.Rotation,
                            Scale = localTransform.GetRefRO(fishEntity).ValueRO.Scale
                        });
                        break;
                    case WallType.Floor:
                        ecb.SetComponent<LocalTransform>(fishEntity, new LocalTransform
                        {
                            Position = localTransform.GetRefRW(fishEntity).ValueRW.Position - new float3(0,10,0),
                            Rotation = localTransform.GetRefRO(fishEntity).ValueRO.Rotation,
                            Scale = localTransform.GetRefRO(fishEntity).ValueRO.Scale
                        });
                        break;
                    case WallType.Left:
                        ecb.SetComponent<LocalTransform>(fishEntity, new LocalTransform
                        {
                            Position = localTransform.GetRefRW(fishEntity).ValueRW.Position - new float3(10,0,0),
                            Rotation = localTransform.GetRefRO(fishEntity).ValueRO.Rotation,
                            Scale = localTransform.GetRefRO(fishEntity).ValueRO.Scale
                        });
                        break;
                    case WallType.Right:
                        ecb.SetComponent<LocalTransform>(fishEntity, new LocalTransform
                        {
                            Position = localTransform.GetRefRW(fishEntity).ValueRW.Position - new float3(-10,0,0),
                            Rotation = localTransform.GetRefRO(fishEntity).ValueRO.Rotation,
                            Scale = localTransform.GetRefRO(fishEntity).ValueRO.Scale
                        });
                        break;
                    case WallType.Front:
                        ecb.SetComponent<LocalTransform>(fishEntity, new LocalTransform
                        {
                            Position = localTransform.GetRefRW(fishEntity).ValueRW.Position - new float3(0,0,10),
                            Rotation = localTransform.GetRefRO(fishEntity).ValueRO.Rotation,
                            Scale = localTransform.GetRefRO(fishEntity).ValueRO.Scale
                        });
                        break;
                    case WallType.Back:
                        ecb.SetComponent<LocalTransform>(fishEntity, new LocalTransform
                        {
                            Position = localTransform.GetRefRW(fishEntity).ValueRW.Position - new float3(0,0,-10),
                            Rotation = localTransform.GetRefRO(fishEntity).ValueRO.Rotation,
                            Scale = localTransform.GetRefRO(fishEntity).ValueRO.Scale
                        });
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
    }
}
