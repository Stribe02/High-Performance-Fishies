using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Random = UnityEngine.Random;

[CreateAfter(typeof(PredatorSpawnSystem))]
partial struct PredatorMovementSystem : ISystem
{
    float3 targetPos;
    float3 targetPosRun;

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var config = SystemAPI.GetSingleton<Config>();

        NativeArray<float3> targetPosResult = new NativeArray<float3>(1, Allocator.TempJob);
        targetPosResult[0] = targetPos;
        var predatorMoveJob = new PredatorMoveJob
        {
            targetPos = targetPosResult,
            dt = Time.deltaTime,
            randomVector = new float3(Random.Range(-100, 100), Random.Range(-100, 100), Random.Range(-100, 100))
        };

        JobHandle predatorMoveHandle = default;
        switch (config.ScheduleType)
        {
            case ScheduleType.Schedule:
                predatorMoveHandle = predatorMoveJob.Schedule(state.Dependency);
                break;
            case ScheduleType.ScheduleParallel:
                predatorMoveHandle = predatorMoveJob.ScheduleParallel(state.Dependency);
                break;
            case ScheduleType.Run: 
                foreach (var (transform, entity) in SystemAPI.Query<RefRW<LocalTransform>>().WithAll<PredatorTag>().WithEntityAccess())
                {
                    float3 dir = math.normalize(targetPosRun - transform.ValueRO.Position);

                    if (!dir.Equals(float3.zero))
                    {
                        quaternion rot = quaternion.LookRotation(dir, new float3(0, 1, 0));
                        transform.ValueRW.Rotation = math.slerp(transform.ValueRW.Rotation, rot, 2f * Time.deltaTime);
                    }

                    transform.ValueRW.Position += transform.ValueRW.Forward() * 15f * Time.deltaTime;

                    if (math.distance(transform.ValueRW.Position, targetPosRun) < 5f)
                    {
                        targetPosRun = new float3(Random.Range(-100, 100), Random.Range(-100, 100), Random.Range(-100, 100));
                    }
                }
                break;
            default:
                break;
        }
        predatorMoveHandle.Complete();

        targetPos = targetPosResult[0];
        targetPosResult.Dispose();
    }

    [BurstCompile]
    [WithAll(typeof(PredatorTag))]
    partial struct PredatorMoveJob : IJobEntity
    {
        public NativeArray<float3> targetPos;
        public float dt;
        public float3 randomVector;
        public void Execute(ref LocalTransform transform)
        {
            float3 dir = math.normalize(targetPos[0] - transform.Position);
        
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
