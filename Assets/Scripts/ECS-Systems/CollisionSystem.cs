using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Physics;
using Unity.Transforms;

//[UpdateBefore(typeof(PhysicsSimulationGroup))]
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[CreateAfter(typeof(FishSchoolSpawner))]
partial struct CollisionSystem : ISystem
{
    
    ComponentLookup<RockComponent> RockComponentLookup;
    ComponentLookup<WallTag> wallLookup;
    ComponentLookup<FishAttributes> fishLookup;
    ComponentLookup<LocalTransform> localTransformUp;
    ComponentLookup<FishSchoolAttribute> fishSchoolAttributeLookup;
    EntityQuery query_schools;

    
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<SimulationSingleton>();
        state.RequireForUpdate<PhysicsWorldSingleton>();
        state.RequireForUpdate<Config>();
        state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        RockComponentLookup = state.GetComponentLookup<RockComponent>();
        wallLookup = state.GetComponentLookup<WallTag>(true);
        fishLookup = state.GetComponentLookup<FishAttributes>();
        localTransformUp = state.GetComponentLookup<LocalTransform>();
        fishSchoolAttributeLookup = state.GetComponentLookup<FishSchoolAttribute>();
        query_schools = new EntityQueryBuilder(Allocator.Temp).WithAll<FishSchoolAttribute>().Build(ref state);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        RockComponentLookup.Update(ref state);
        wallLookup.Update(ref state);
        fishLookup.Update(ref state);
        localTransformUp.Update(ref state);
        fishSchoolAttributeLookup.Update(ref state);

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
            
            state.Dependency.Complete();
            NativeList<Entity> tempList = query_schools.ToEntityListAsync(Allocator.TempJob, out JobHandle fishSchoolTest);
            state.Dependency = new CheckCollisionFishJob
            {
                fish = fishLookup,
                wall = wallLookup,
                PhysicsWorldSingleton = physicsWorldSingleton,
                fishSchools = tempList,
                fishSchoolAttribute = fishSchoolAttributeLookup
            }.Schedule(SystemAPI.GetSingleton<SimulationSingleton>(), fishSchoolTest);

            state.Dependency.Complete();
            tempList.Dispose();
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

    [BurstCompile]
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
    
    [BurstCompile]
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
            
            // based on the fish SchoolIndex -> set that schools fishHasHitWall and Position to move away from
            foreach (var fishSchoolAttribute in SystemAPI
                         .Query<RefRW<FishSchoolAttribute>>())
            {
                if (fishSchoolAttribute.ValueRO.SchoolIndex !=
                    SystemAPI.GetComponentRO<FishAttributes>(fishEntity).ValueRO.SchoolIndex) continue;
                fishSchoolAttribute.ValueRW.PosToMoveAwayFrom = avgContactPointPosition;
                fishSchoolAttribute.ValueRW.FishHasHitWall = true;
            }
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
        // we need to look entities with fishes
        public ComponentLookup<FishAttributes> fish;
        [ReadOnly] public ComponentLookup<WallTag> wall;
        public NativeList<Entity> fishSchools;
        public ComponentLookup<FishSchoolAttribute> fishSchoolAttribute;
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
                int fishSchoolIndex = fish.GetRefRO(fishEntity).ValueRO.SchoolIndex;
                foreach (var school in fishSchools)
                {
                    if (fishSchoolAttribute.GetRefRO(school).ValueRO.SchoolIndex == fishSchoolIndex)
                    {
                        fishSchoolAttribute.GetRefRW(school).ValueRW.FishHasHitWall = true;
                        fishSchoolAttribute.GetRefRW(school).ValueRW.PosToMoveAwayFrom = avgContactPointPosition;
                    }
                }
        }
    }
}
