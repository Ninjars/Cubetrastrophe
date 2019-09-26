using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

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
