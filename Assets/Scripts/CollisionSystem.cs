using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using UnityEngine;

partial struct CollisionSystem : ISystem
{
    //TODO ADD BURSTCOMPILE BACK
    ComponentLookup<RockComponent> RockComponentLookup;
    ComponentLookup<WallTag> wallLookup;
    ComponentLookup<FishAttributes> fishLookup;
    
    
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<SimulationSingleton>();
        state.RequireForUpdate<PhysicsWorldSingleton>();
        RockComponentLookup = state.GetComponentLookup<RockComponent>();
        wallLookup = state.GetComponentLookup<WallTag>(true);
        fishLookup = state.GetComponentLookup<FishAttributes>();
    }

    
    public void OnUpdate(ref SystemState state)
    {
        
        RockComponentLookup.Update(ref state);
        wallLookup.Update(ref state);
        fishLookup.Update(ref state);

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
            PhysicsWorldSingleton = physicsWorldSingleton
        }.Schedule(SystemAPI.GetSingleton<SimulationSingleton>(), state.Dependency);
    }
    
    
    
    public partial struct CheckCollisionRock : ICollisionEventsJob
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
        [ReadOnly] public ComponentLookup<WallTag> wall;
        [ReadOnly] public PhysicsWorldSingleton PhysicsWorldSingleton;

        public void Execute(CollisionEvent collisionEvent)
        {
            Entity fishEntity;
            
            // one collision detect, rock can be either entity A or B, so check for both.
            // Is rock hitting a wall?
            if (fish.HasComponent(collisionEvent.EntityA) && wall.HasComponent(collisionEvent.EntityB))
            {
                fishEntity = collisionEvent.EntityA;
            }
            else if (fish.HasComponent(collisionEvent.EntityB) && wall.HasComponent(collisionEvent.EntityA))
            {
                fishEntity = collisionEvent.EntityB;
            }
            else fishEntity = Entity.Null;

            if (!fishEntity.Equals(Entity.Null))
            {
                /*
                // Stuff
                var collisionDetails = collisionEvent.CalculateDetails(ref PhysicsWorldSingleton.PhysicsWorld);
                Debug.Log("We have a fish");
                
                foreach(var contactPosition in collisionDetails.EstimatedContactPointPositions)
                {
                    Debug.Log($"{contactPosition}");
                }*/
            }
        }
    }
}
