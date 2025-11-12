using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;


partial struct FishSchoolSpawner : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<Config>();
        state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton
        >();
    }

    public void OnUpdate(ref SystemState state)
    {

        state.Enabled = false;

        var config = SystemAPI.GetSingleton<Config>();
        var random = new Random(123);
        var ecb =
            SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton
            >().CreateCommandBuffer(state.WorldUnmanaged);

        for (int i = 0; i < config.NumberOfSchools; i++)
        {
            // FISH SCHOOL HAVE THEIR OWN ENTITY FOR THEIR SCHOOL
            var fishSchoolEntity = state.EntityManager.CreateEntity();
            state.EntityManager.SetName(fishSchoolEntity, "FishSchool " + i);
            state.EntityManager.AddComponent<FishSchoolAttribute>(fishSchoolEntity);
            state.EntityManager.SetComponentData<FishSchoolAttribute>(fishSchoolEntity, new FishSchoolAttribute
                {
                    SchoolIndex = i,
                    FlockSize = config.FlockSize,
                    CohesionWeight = config.DefaultCohesionWeight,
                    SeparationWeight = config.DefaultSeparationWeight,
                    AlignmentWeight = config.DefaultAlignmentWeight,
                    SeparationRadius = config.DefaultSeparationRadius,
                    FishPrefab = config.SmallFish
                }
            );
            var fishSchoolData = state.EntityManager.GetComponentData<FishSchoolAttribute>(fishSchoolEntity);


            // URPMaterialPropertyBaseColor is a component from the Entities.Graphics package 
            // that lets us set the rendered base color of a rendered entity.
            var color = new URPMaterialPropertyBaseColor { Value = RandomColor(ref random) };

            var fishes =
                state.EntityManager.Instantiate(fishSchoolData.FishPrefab, fishSchoolData.FlockSize, Allocator.Temp);

            foreach (var fish in fishes)
            {

                var transform = SystemAPI.GetComponentRW<LocalTransform>(fish);
                ecb.AddComponent<FishAttributes>(fish);
                transform.ValueRW.Position = random.NextFloat3(new float3(10, 10, 10)); // setting the random location
                ecb.AddComponent<AquaticAnimalAttributes>(fish);
                SystemAPI.GetComponentRW<AquaticAnimalAttributes>(fish).ValueRW.Speed =
                    2f; // making sure the speed is set
                // Every root entity instantiated from a prefab has a LinkedEntityGroup component, which
                // is a list of all the entities that make up the prefab hierarchy (including the root).

                // (LinkedEntityGroup is a special kind of component called a "DynamicBuffer", which is
                // a resizable array of struct values instead of just a single struct.)
                var linkedEntities = state.EntityManager.GetBuffer<LinkedEntityGroup>(fish);
                foreach (var entity in linkedEntities)
                {
                    // We want to set the URPMaterialPropertyBaseColor component only on the
                    // entities that have it, so we first check.
                    if (state.EntityManager.HasComponent<URPMaterialPropertyBaseColor>(entity.Value))
                    {
                        // Set the color of each entity that makes up the tank.
                        state.EntityManager.SetComponentData(entity.Value, color);
                    }
                }
            }
        }
    }
    /*   var fishPrefab = SystemAPI.GetSingleton<FishSchoolAttribute>().FishPrefab;
       // random pos for now:
       var random = new Random(123);
       var ecb =
           SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton
           >().CreateCommandBuffer(state.WorldUnmanaged);
       // for each fishSchool: Spawn fish according to the flocksize
       foreach (var fishSchool in fischSchoolInstances)
       {
           var color = new URPMaterialPropertyBaseColor
               { Value = RandomColor(ref random) }; // give School random Color
           var flockSize = SystemAPI.GetComponentRO<FishSchoolAttribute>(fishSchool).ValueRO.FlockSize;

           // spawn fishes based on the flocksize
           var fishes =
               state.EntityManager.Instantiate(fishPrefab, flockSize, Allocator.Temp);
           // Add fish entities to FishSchool NattiveArray
           SystemAPI.GetComponentRW<FishSchoolAttribute>(fishSchool).ValueRW.Fishes = fishes;


           // Loop over fish
           foreach (var fish in fishes)
           {

               var transform = SystemAPI.GetComponentRW<LocalTransform>(fish);
               ecb.AddComponent<FishAttributes>(fish);
               transform.ValueRW.Position = random.NextFloat3(new float3(10, 10, 10)); // setting the random location
               ecb.AddComponent<AquaticAnimalAttributes>(fish);
               SystemAPI.GetComponentRW<AquaticAnimalAttributes>(fish).ValueRW.Speed =
                   2f; // making sure the speed is set
               // Every root entity instantiated from a prefab has a LinkedEntityGroup component, which
               // is a list of all the entities that make up the prefab hierarchy (including the root).

               // (LinkedEntityGroup is a special kind of component called a "DynamicBuffer", which is
               // a resizable array of struct values instead of just a single struct.)
               var linkedEntities = state.EntityManager.GetBuffer<LinkedEntityGroup>(fish);
               foreach (var entity in linkedEntities)
               {
                   // We want to set the URPMaterialPropertyBaseColor component only on the
                   // entities that have it, so we first check.
                   if (state.EntityManager.HasComponent<URPMaterialPropertyBaseColor>(entity.Value))
                   {
                       // Set the color of each entity that makes up the tank.
                       state.EntityManager.SetComponentData(entity.Value, color);
                   }
               }
           }
       }
   }*/

            // Return a random color that is visually distinct.
            // (Naive randomness would produce a distribution of colors clustered 
            // around a narrow range of hues. See https://martin.ankerl.com/2009/12/09/how-to-create-random-colors-programmatically/ )
            static float4 RandomColor(ref Random random)
            {
                // 0.618034005f is inverse of the golden ratio
                var hue = (random.NextFloat() + 0.618034005f) % 1;
                return (Vector4)Color.HSVToRGB(hue, 1.0f, 1.0f);
            }
        }


