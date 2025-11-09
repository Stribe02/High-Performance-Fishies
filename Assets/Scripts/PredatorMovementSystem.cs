//using System.Numerics;
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

//currently only used for random.range, should be replaced when optimizing
using UnityEngine;

partial struct PredatorMovementSystem : ISystem
{
    Vector3 targetPos;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<PredatorTag>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
  
        foreach (var (transform, entity) in SystemAPI.Query<RefRW<LocalTransform>>().WithAll<PredatorTag>().WithEntityAccess()) {
           
            Vector3 dir = (targetPos - new Vector3(transform.ValueRW.Position.x, transform.ValueRW.Position.y, transform.ValueRW.Position.z)).normalized;
        
            if (dir != Vector3.zero)
            {
                Quaternion rot = Quaternion.LookRotation(dir);
                transform.ValueRW.Rotation = Quaternion.Slerp(transform.ValueRW.Rotation, rot, 2f * Time.deltaTime);
            }

            transform.ValueRW.Position += transform.ValueRW.Forward() * 15f * Time.deltaTime;

            if (Vector3.Distance(transform.ValueRW.Position, targetPos) < 5f)
            {
                targetPos = new Vector3(Random.Range(-100, 100), Random.Range(-100, 100), Random.Range(-100, 100));
            }

        
        }
        
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        
    }
}
