using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

partial struct SmallFishSpawnerSystem : ISystem
{/*

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<FishAttributes>();
    }


    public void OnUpdate(ref SystemState state)
    {
        state.Enabled = false;

        var smallFish = SystemAPI.GetSingleton<FishAttributes>();
        var smallFishPrefab = smallFish.SmallFishPrefab;
        // 10 fishes
        var instances = state.EntityManager.Instantiate(smallFishPrefab, 10, Allocator.Temp);
        // random pos for now:
        var random = new Random(123);
        var ecb =
            SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton
            >().CreateCommandBuffer(state.WorldUnmanaged);
        foreach (var fish in instances)
        {
            var transform = SystemAPI.GetComponentRW<LocalTransform>(fish);
            ecb.AddComponent<FishAttributes>(fish);
            transform.ValueRW.Position = random.NextFloat3(new float3(10, 10, 10)); // setting the random location
            ecb.AddComponent<AquaticAnimalAttributes>(fish);
            SystemAPI.GetComponentRW<AquaticAnimalAttributes>(fish).ValueRW.Speed = 5f;
        }
    }
    
    public void OnDestroy(ref SystemState state)
    {
        
    }*/
}
