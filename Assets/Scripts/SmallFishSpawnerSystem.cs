using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

partial struct SmallFishSpawnerSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<SmallFishSpawner>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        state.Enabled = false;

        var smallFishPrefab = SystemAPI.GetSingleton<SmallFishSpawner>().SmallFishPrefab;
        // 10 fishes
        var instances = state.EntityManager.Instantiate(smallFishPrefab, 10, Allocator.Temp);
        // random pos for now:
        var random = new Random(123);
        foreach (var fish in instances)
        {
            var transform = SystemAPI.GetComponentRW<LocalTransform>(fish);
            transform.ValueRW.Position = random.NextFloat3(new float3(10, 10, 10)); // setting the random location
        }
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        
    }
}
