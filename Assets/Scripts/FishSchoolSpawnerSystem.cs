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
        state.RequireForUpdate<FishSchoolAttribute>();
    }

    public void OnUpdate(ref SystemState state)
    {

        state.Enabled = false;
        // Random Number
        var random = new Random(123);
        var fishSchoolTest = SystemAPI.GetSingleton<FishSchoolAttribute>();


        var ecb =
            SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton
            >().CreateCommandBuffer(state.WorldUnmanaged);

        for (int i = 0; i < fishSchoolTest.SchoolIndex; i++)
        {
            // URPMaterialPropertyBaseColor is a component from the Entities.Graphics package 
            // that lets us set the rendered base color of a rendered entity.
            var color = new URPMaterialPropertyBaseColor { Value = RandomColor(ref random) };
            
            // spawn fishes based on the flocksize
            var fishes =
                state.EntityManager.Instantiate(fishSchoolTest.FishPrefab, fishSchoolTest.FlockSize, Allocator.Temp);
            foreach (var fish in fishes)
            {
                var transform = SystemAPI.GetComponentRW<LocalTransform>(fish);
                ecb.AddComponent<FishAttributes>(fish);
                SystemAPI.GetComponentRW<FishAttributes>(fish).ValueRW.SchoolIndex = i; // setting the schoolindex on the fish
                transform.ValueRW.Position = random.NextFloat3(new float3(10, 10, 10)); // setting the random location
                ecb.AddComponent<AquaticAnimalAttributes>(fish);
                SystemAPI.GetComponentRW<AquaticAnimalAttributes>(fish).ValueRW.Speed = 2f; // making sure the speed is set
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

