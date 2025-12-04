using Unity.Burst;
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
        var sharkEntity = state.EntityManager.Instantiate(shark.Prefab);

        SystemAPI.GetComponentRW<LocalTransform>(sharkEntity).ValueRW.Position = new float3(0, -10, -10);
        state.EntityManager.AddComponent<PredatorTag>(sharkEntity);
        state.EntityManager.AddComponentData<AquaticAnimalAttributes>(sharkEntity, new AquaticAnimalAttributes
        {
            Speed = 2f,
            Radius = 3f
        });
    }
}
