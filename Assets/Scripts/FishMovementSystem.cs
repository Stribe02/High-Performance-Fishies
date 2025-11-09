using Unity.Burst;
using Unity.Entities;

partial struct FishMovementSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        
    }
    
    //var ECB =
    //SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton
   // >().CreateCommandBuffer(state.WorldUnmanaged)

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        
    }
}
