using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public struct TargetTag : IComponentData {}
public struct DefenderTag : IComponentData {}

public struct HasTarget : IComponentData {
    public Entity targetEntity;
    public float3 targetPosition;
    public float refreshTargetPeriod;
    public float elapsedRefreshTime;
}

public class HasTargetDebug : ComponentSystem {
    protected override void OnUpdate() {
        Entities.ForEach((Entity entity, ref Translation translation, ref HasTarget hasTarget) => {
            Debug.DrawLine(translation.Value, hasTarget.targetPosition);
        });
    }
}
