//using System.Numerics;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

//currently only used for random.range, should be replaced when optimizing
using UnityEngine;
using Random = UnityEngine.Random;

[CreateAfter(typeof(PredatorSpawnSystem))]
partial struct PredatorMovementSystem : ISystem
{
    float3 targetPos;

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        NativeArray<float3> targetPosResult = new NativeArray<float3>(1, Allocator.TempJob);
        targetPosResult[0] = targetPos;
        var jobHandle = new PredatorMoveJob
        {
            targetPos = targetPosResult,
            dt = Time.deltaTime,
            randomVector = new float3(Random.Range(-100, 100), Random.Range(-100, 100), Random.Range(-100, 100))
        }.Schedule(state.Dependency);
        
        jobHandle.Complete();
        targetPos = targetPosResult[0];
        targetPosResult.Dispose();

    }

    [WithAll(typeof(PredatorTag))]
    partial struct PredatorMoveJob : IJobEntity
    {
        public NativeArray<float3> targetPos;
        public float dt;
        public float3 randomVector;
        public void Execute(ref LocalTransform transform)
        {
            float3 dir = math.normalize(targetPos[0] - new float3(transform.Position.x, transform.Position.y, transform.Position.z));
        
            if (!dir.Equals(float3.zero))
            {
                quaternion rot = quaternion.LookRotation(dir, new float3(0,1,0));
                transform.Rotation = math.slerp(transform.Rotation, rot, 2f * dt);
            }

            transform.Position += transform.Forward() * 15f * dt;

            if (math.distance(transform.Position, targetPos[0]) < 5f)
            {
                targetPos[0] = randomVector;
            }
        }
    }
}
