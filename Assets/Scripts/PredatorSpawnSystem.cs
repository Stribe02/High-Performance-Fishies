using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

partial struct PredatorSpawnSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<PredatorShark>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        state.Enabled = false;

        var shark = SystemAPI.GetSingleton<PredatorShark>();
        var instances = state.EntityManager.Instantiate(shark.Prefab, 1, Allocator.Temp);

        var random = new Random(123);
        foreach (var entity in instances)
        {
            var transform = SystemAPI.GetComponentRW<LocalTransform>(entity);
            transform.ValueRW.Position = random.NextFloat3(new float3(10, 10, 10));
        }


    }


}
