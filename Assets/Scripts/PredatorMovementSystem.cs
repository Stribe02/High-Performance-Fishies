using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

//currently only used for random.range, should be replaced when optimizing
using UnityEngine;

[CreateAfter(typeof(PredatorSpawnSystem))]
partial struct PredatorMovementSystem : ISystem
{
    Vector3 targetPos;

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        NativeArray<Vector3> targetPosResult = new NativeArray<Vector3>(1, Allocator.TempJob);
        targetPosResult[0] = targetPos;
        var jobHandle = new PredatorMoveJob
        {
            targetPos = targetPosResult,
            dt = Time.deltaTime,
            randomVector = new Vector3(Random.Range(-100, 100), Random.Range(-100, 100), Random.Range(-100, 100))
        }.Schedule(state.Dependency);
        
        jobHandle.Complete();
        targetPos = targetPosResult[0];
        targetPosResult.Dispose();

    }

    [WithAll(typeof(PredatorTag))]
    partial struct PredatorMoveJob : IJobEntity
    {
        public NativeArray<Vector3> targetPos;
        public float dt;
        public Vector3 randomVector;
        public void Execute(ref LocalTransform transform)
        {
            Vector3 dir = (targetPos[0] - new Vector3(transform.Position.x, transform.Position.y, transform.Position.z)).normalized;
        
            if (dir != Vector3.zero)
            {
                Quaternion rot = Quaternion.LookRotation(dir);
                transform.Rotation = Quaternion.Slerp(transform.Rotation, rot, 2f * dt);
            }

            transform.Position += transform.Forward() * 15f * dt;

            if (Vector3.Distance(transform.Position, targetPos[0]) < 5f)
            {
                targetPos[0] = randomVector;
            }
        }
    }    
}
