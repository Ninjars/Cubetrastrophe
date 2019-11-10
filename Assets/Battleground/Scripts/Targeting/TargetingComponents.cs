using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

interface TeamTag {
    void AssignToEntity(EntityManager manager, Entity entity);
}

public struct BTeamTag : TeamTag, IComponentData {
    public void AssignToEntity(EntityManager manager, Entity entity) {
        manager.AddComponentData(entity, this);
    }
}
public struct ATeamTag : TeamTag, IComponentData {
    public void AssignToEntity(EntityManager manager, Entity entity) {
        manager.AddComponentData(entity, this);
    }
}

public struct NoTeamTag : TeamTag {
    public void AssignToEntity(EntityManager manager, Entity entity) {
        // no action
    }
}

public enum Team {
    NONE,
    TEAM_A,
    TEAM_B,
}

static class TeamExtensions {
    public static TeamTag toComponent(this Team team) {
        switch(team) {
            case Team.TEAM_A: return new ATeamTag{};
            case Team.TEAM_B: return new BTeamTag{};
            default: return new NoTeamTag{};
        }
    }
}

public struct HasTarget : IComponentData {
    public Entity targetEntity;
    public float3 targetPosition;
    public float3 targetVelocity;
    public float refreshTargetPeriod;
    public float elapsedRefreshTime;
}

public class HasTargetDebug : ComponentSystem {
    protected override void OnUpdate() {
        Entities.ForEach((Entity entity, ref LocalToWorld translation, ref HasTarget hasTarget) => {
            Debug.DrawLine(translation.Position, hasTarget.targetPosition);
        });
    }
}
