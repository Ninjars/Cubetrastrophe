using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

// developed with reference to Code Monkey at https://www.youtube.com/watch?v=nuxTq0AQAyY
static class TargetingSystemUtils {
    public struct EntityWithPosition {
        public Entity entity;
        public float3 position;
    }

    public static EntityWithPosition getClosestTarget(NativeArray<EntityWithPosition> targetArray, ref Translation translation) {
        Entity closest = Entity.Null;
        float3 unitPosition = translation.Value;
        float3 matchedTargetPosition = float3.zero;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < targetArray.Length; i++) {
            var item = targetArray[i];
            var targetEntity = item.entity;
            var targetPosition = item.position;
            var distance = math.distancesq(unitPosition, targetPosition);

            if (closest == null) {
                closest = targetEntity;
                bestDistance = distance;
                matchedTargetPosition = targetPosition;

            } else {
                if (distance < bestDistance) {
                    closest = targetEntity;
                    bestDistance = distance;
                    matchedTargetPosition = targetPosition;
                }
            }
        }
        return new EntityWithPosition { entity = closest, position = matchedTargetPosition };
    }

    public static void assignTarget(
            NativeArray<EntityWithPosition> closestTargetArray,
            EntityCommandBuffer.Concurrent commandBuffer,
            Entity entity,
            int index) {
        var target = closestTargetArray[index];
        if (target.entity != Entity.Null) {
            commandBuffer.AddComponent(index, entity, new HasTarget {
                targetEntity = target.entity,
                targetPosition = target.position,
                refreshTargetPeriod = 2f,
                elapsedRefreshTime = 0f,
            });
        }
    }
}

public class TeamAFindTargetJobSystem : JobComponentSystem {
    private EndSimulationEntityCommandBufferSystem endSimCommandBufferSystem;
    private EntityQuery targetsQuery;
    private EntityQuery searchersQuery;

    [RequireComponentTag(typeof(ATeamTag))]
    [ExcludeComponent(typeof(HasTarget))]
    [BurstCompile]
    private struct FindTargetJob : IJobForEachWithEntity<Translation> {

        [DeallocateOnJobCompletion]
        [ReadOnly]
        public NativeArray<TargetingSystemUtils.EntityWithPosition> targetArray;
        public NativeArray<TargetingSystemUtils.EntityWithPosition> closestTargetArray;

        public void Execute(Entity entity, int index, [ReadOnly] ref Translation translation) {
            closestTargetArray[index] = TargetingSystemUtils.getClosestTarget(targetArray, ref translation);
        }
    }

    [RequireComponentTag(typeof(ATeamTag))]
    [ExcludeComponent(typeof(HasTarget))]
    private struct AssignTargetJob : IJobForEachWithEntity<Translation> {

        [DeallocateOnJobCompletion]
        [ReadOnly]
        public NativeArray<TargetingSystemUtils.EntityWithPosition> closestTargetArray;
        public EntityCommandBuffer.Concurrent commandBuffer;

        public void Execute(Entity entity, int index, [ReadOnly] ref Translation translation) {
            TargetingSystemUtils.assignTarget(closestTargetArray, commandBuffer, entity, index);
        }
    }

    protected override void OnCreate() {
        endSimCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        targetsQuery = GetEntityQuery(typeof(BTeamTag), ComponentType.ReadOnly<Translation>());
        searchersQuery = GetEntityQuery(typeof(ATeamTag), ComponentType.Exclude<HasTarget>());
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps) {
        // First gather the state data we want to operate on.
        // We want both entity and translation information about the target, so extract this from the target query.
        var targetEntities = targetsQuery.ToEntityArray(Allocator.TempJob);
        var targetTranslations = targetsQuery.ToComponentDataArray<Translation>(Allocator.TempJob);

        // Construct a new array that merges the two data types together into simple structs.
        var targetArray = new NativeArray<TargetingSystemUtils.EntityWithPosition>(targetEntities.Length, Allocator.TempJob);
        for (int i = 0; i < targetArray.Length; i++) {
            targetArray[i] = new TargetingSystemUtils.EntityWithPosition {
                entity = targetEntities[i],
                position = targetTranslations[i].Value,
            };
        }

        // Clear up the intermediate native arrays; the native arrays passed to jobs are marked to be automatically deallocated.
        targetEntities.Dispose();
        targetTranslations.Dispose();

        // We need to extract the closest target per searching unit, so we need an array whose length matches the unit count.
        var closestTargetArray = new NativeArray<TargetingSystemUtils.EntityWithPosition>(searchersQuery.CalculateEntityCount(), Allocator.TempJob);
        var findTargetJob = new FindTargetJob {
            targetArray = targetArray,
            closestTargetArray = closestTargetArray,
        };

        // This separation is necessary because command buffers can't be used in Burst compiled structs.
        // We pass a reference to the same closestTargetArray to this job too, with the intention to read from it.
        // This requires the sequencing of the jobs to be correct.
        var assignTargetJob = new AssignTargetJob {
            closestTargetArray = closestTargetArray,
            commandBuffer = endSimCommandBufferSystem.CreateCommandBuffer().ToConcurrent(),
        };

        // This makes the "find target" job a prerequisite for the "assign target" job, to ensure the closestTargetArray is initiated.
        var jobHandle = findTargetJob.Schedule(this, inputDeps);
        jobHandle = assignTargetJob.Schedule(this, jobHandle);

        endSimCommandBufferSystem.AddJobHandleForProducer(jobHandle);
        return jobHandle;
    }
}

public class TeamBFindTargetJobSystem : JobComponentSystem {
    private EndSimulationEntityCommandBufferSystem endSimCommandBufferSystem;
    private EntityQuery targetsQuery;
    private EntityQuery searchersQuery;

    [RequireComponentTag(typeof(BTeamTag))]
    [ExcludeComponent(typeof(HasTarget))]
    [BurstCompile]
    private struct FindTargetJob : IJobForEachWithEntity<Translation> {

        [DeallocateOnJobCompletion]
        [ReadOnly]
        public NativeArray<TargetingSystemUtils.EntityWithPosition> targetArray;
        public NativeArray<TargetingSystemUtils.EntityWithPosition> closestTargetArray;

        public void Execute(Entity entity, int index, [ReadOnly] ref Translation translation) {
            closestTargetArray[index] = TargetingSystemUtils.getClosestTarget(targetArray, ref translation);
        }
    }

    [RequireComponentTag(typeof(BTeamTag))]
    [ExcludeComponent(typeof(HasTarget))]
    private struct AssignTargetJob : IJobForEachWithEntity<Translation> {

        [DeallocateOnJobCompletion]
        [ReadOnly]
        public NativeArray<TargetingSystemUtils.EntityWithPosition> closestTargetArray;
        public EntityCommandBuffer.Concurrent commandBuffer;

        public void Execute(Entity entity, int index, [ReadOnly] ref Translation translation) {
            TargetingSystemUtils.assignTarget(closestTargetArray, commandBuffer, entity, index);
        }
    }

    protected override void OnCreate() {
        endSimCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        targetsQuery = GetEntityQuery(typeof(ATeamTag), ComponentType.ReadOnly<Translation>());
        searchersQuery = GetEntityQuery(typeof(BTeamTag), ComponentType.Exclude<HasTarget>());
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps) {
        // First gather the state data we want to operate on.
        // We want both entity and translation information about the target, so extract this from the target query.
        var targetEntities = targetsQuery.ToEntityArray(Allocator.TempJob);
        var targetTranslations = targetsQuery.ToComponentDataArray<Translation>(Allocator.TempJob);

        // Construct a new array that merges the two data types together into simple structs.
        var targetArray = new NativeArray<TargetingSystemUtils.EntityWithPosition>(targetEntities.Length, Allocator.TempJob);
        for (int i = 0; i < targetArray.Length; i++) {
            targetArray[i] = new TargetingSystemUtils.EntityWithPosition {
                entity = targetEntities[i],
                position = targetTranslations[i].Value,
            };
        }

        // Clear up the intermediate native arrays; the native arrays passed to jobs are marked to be automatically deallocated.
        targetEntities.Dispose();
        targetTranslations.Dispose();

        // We need to extract the closest target per searching unit, so we need an array whose length matches the unit count.
        var closestTargetArray = new NativeArray<TargetingSystemUtils.EntityWithPosition>(searchersQuery.CalculateEntityCount(), Allocator.TempJob);
        var findTargetJob = new FindTargetJob {
            targetArray = targetArray,
            closestTargetArray = closestTargetArray,
        };

        // This separation is necessary because command buffers can't be used in Burst compiled structs.
        // We pass a reference to the same closestTargetArray to this job too, with the intention to read from it.
        // This requires the sequencing of the jobs to be correct.
        var assignTargetJob = new AssignTargetJob {
            closestTargetArray = closestTargetArray,
            commandBuffer = endSimCommandBufferSystem.CreateCommandBuffer().ToConcurrent(),
        };

        // This makes the "find target" job a prerequisite for the "assign target" job, to ensure the closestTargetArray is initiated.
        var jobHandle = findTargetJob.Schedule(this, inputDeps);
        jobHandle = assignTargetJob.Schedule(this, jobHandle);

        endSimCommandBufferSystem.AddJobHandleForProducer(jobHandle);
        return jobHandle;
    }
}

public class RefreshHasTargetSystem : JobComponentSystem {
    private EndSimulationEntityCommandBufferSystem endSimCommandBufferSystem;

    private struct RefreshTargetJob : IJobForEachWithEntity<HasTarget> {
        public EntityCommandBuffer.Concurrent commandBuffer;
        public float deltaTime;
        [ReadOnly]
        public ComponentDataFromEntity<Translation> translationType;

        public void Execute(Entity entity, int index, ref HasTarget hasTarget) {
            hasTarget.elapsedRefreshTime += deltaTime;
            if (hasTarget.elapsedRefreshTime > hasTarget.refreshTargetPeriod) {
                commandBuffer.RemoveComponent<HasTarget>(index, entity);
            } else {
                hasTarget.targetPosition = translationType[hasTarget.targetEntity].Value;
            }
        }
    }

    protected override void OnCreate() {
        endSimCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps) {
        var job = new RefreshTargetJob {
            commandBuffer = endSimCommandBufferSystem.CreateCommandBuffer().ToConcurrent(),
            deltaTime = Time.deltaTime,
            translationType = GetComponentDataFromEntity<Translation>(true),
        };

        var jobHandle = job.Schedule(this, inputDeps);
        endSimCommandBufferSystem.AddJobHandleForProducer(jobHandle);
        return jobHandle;
    }
}

/*
// kept for comparison
public class HasTargetRefresh : ComponentSystem {
    protected override void OnUpdate() {
        var time = Time.deltaTime;
        Entities.ForEach((Entity entity, ref Translation translation, ref HasTarget hasTarget) => {
            hasTarget.elapsedRefreshTime += time;
            if (hasTarget.elapsedRefreshTime > hasTarget.refreshTargetPeriod) {
                PostUpdateCommands.RemoveComponent<HasTarget>(entity);
            } else {
                hasTarget.targetPosition = World.Active.EntityManager.GetComponentData<Translation>(hasTarget.targetEntity).Value;
            }
        });
    }
}
*/

/*
// kept for comparison
public class NearestTargetSystem : ComponentSystem {
    protected override void OnUpdate() {
        Entities.WithNone<HasTarget>().WithAll<DefenderTag>().ForEach((Entity entity, ref Translation entityTranslation) => {
            Entity closest = Entity.Null;
            float3 unitPosition = entityTranslation.Value;
            float3 targetPosition = float3.zero;
            float bestDistance = float.MaxValue;

            Entities.WithAll<TargetTag>().ForEach((Entity targetEntity, ref Translation targetTranslation) => {
                var distance = math.distancesq(unitPosition, targetPosition);

                if (closest == null) {
                    closest = targetEntity;
                    bestDistance = distance;
                    targetPosition = targetTranslation.Value;

                } else {
                    if (distance < bestDistance) {
                        closest = targetEntity;
                        bestDistance = distance;
                        targetPosition = targetTranslation.Value;
                    }
                }
            });

            if (closest != Entity.Null) {
                PostUpdateCommands.AddComponent(entity, new HasTarget {
                    targetEntity = closest,
                    targetPosition = targetPosition,
                    refreshTargetPeriod = 2f,
                    elapsedRefreshTime = 0f,
                });
            }
        });
    }
}
*/
