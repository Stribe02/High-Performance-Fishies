using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

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

        //var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);
        var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);

        var shark = SystemAPI.GetSingleton<PredatorShark>();
        var sharkEntity = state.EntityManager.Instantiate(shark.Prefab);

        var transform = SystemAPI.GetComponentRW<LocalTransform>(sharkEntity);
        transform.ValueRW.Position = new float3(0,-10,-10);

        ecb.AddComponent<PredatorTag>(sharkEntity);
        ecb.AddComponent<AquaticAnimalAttributes>(sharkEntity);
        //ecb.Playback(state.EntityManager);
    }


}
