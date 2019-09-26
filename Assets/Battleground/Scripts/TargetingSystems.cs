using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class FindTargetJobSystem : JobComponentSystem {
    private EndSimulationEntityCommandBufferSystem endSimCommandBufferSystem;
    private EntityQuery targetQuery;
    private EntityQuery searchingUnitQuery;

    private struct EntityWithPosition {
        public Entity entity;
        public float3 position;
    }

    [RequireComponentTag(typeof(DefenderTag))]
    [ExcludeComponent(typeof(HasTarget))]
    [BurstCompile]
    private struct FindTargetJob : IJobForEachWithEntity<Translation> {

        [DeallocateOnJobCompletion]
        [ReadOnly]
        public NativeArray<EntityWithPosition> targetArray;
        public NativeArray<EntityWithPosition> closestTargetArray;

        public void Execute(Entity entity, int index, [ReadOnly] ref Translation translation) {
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

            closestTargetArray[index] = new EntityWithPosition { entity = closest, position = matchedTargetPosition };
        }
    }

    [RequireComponentTag(typeof(DefenderTag))]
    [ExcludeComponent(typeof(HasTarget))]
    private struct AssignTargetJob : IJobForEachWithEntity<Translation> {

        [DeallocateOnJobCompletion]
        [ReadOnly]
        public NativeArray<EntityWithPosition> closestTargetArray;
        public EntityCommandBuffer.Concurrent commandBuffer;

        public void Execute(Entity entity, int index, [ReadOnly] ref Translation translation) {
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

    protected override void OnCreate() {
        endSimCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        targetQuery = GetEntityQuery(typeof(TargetTag), ComponentType.ReadOnly<Translation>());
        searchingUnitQuery = GetEntityQuery(typeof(DefenderTag), ComponentType.Exclude<HasTarget>());
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps) {
        var targetEntities = targetQuery.ToEntityArray(Allocator.TempJob);
        var targetTranslations = targetQuery.ToComponentDataArray<Translation>(Allocator.TempJob);
        var targetArray = new NativeArray<EntityWithPosition>(targetEntities.Length, Allocator.TempJob);

        for (int i = 0; i < targetArray.Length; i++) {
            targetArray[i] = new EntityWithPosition {
                entity = targetEntities[i],
                position = targetTranslations[i].Value,
            };
        }

        targetEntities.Dispose();
        targetTranslations.Dispose();

        var closestTargetArray = new NativeArray<EntityWithPosition>(searchingUnitQuery.CalculateEntityCount(), Allocator.TempJob);
        var findTargetJob = new FindTargetJob {
            targetArray = targetArray,
            closestTargetArray = closestTargetArray,
        };

        // This separation is necessary because command buffers can't be used in Burst compiled structs
        var assignTargetJob = new AssignTargetJob {
            closestTargetArray = closestTargetArray,
            commandBuffer = endSimCommandBufferSystem.CreateCommandBuffer().ToConcurrent(),
        };

        // this makes the "find target" job a prerequisit for the "assign target" job
        var jobHandle = findTargetJob.Schedule(this, inputDeps);
        jobHandle = assignTargetJob.Schedule(this, jobHandle);

        endSimCommandBufferSystem.AddJobHandleForProducer(jobHandle);
        return jobHandle;
    }
}

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
