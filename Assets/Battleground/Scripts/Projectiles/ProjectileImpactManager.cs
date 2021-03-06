using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

public class ProjectileImpactManager : MonoBehaviour {

    struct ProjectileMiss : IComponentData { }

    public static NativeQueue<ProjectileImpactEvent> queuedProjectileEvents;

    public GameObject dustImpactPrefab;
    public GameObject metalImpactPrefab;
    public GameObject projectileImpactPrefab;

    public static GameObject dustImpactPrefabRef { get; private set; }
    public static GameObject metalImpactPrefabRef { get; private set; }
    public static GameObject projectileImpactPrefabRef { get; private set; }

    private static Dictionary<GameObject, ObjectPool> objectPools;

    private void Awake() {
        queuedProjectileEvents = new NativeQueue<ProjectileImpactEvent>(Allocator.Persistent);
        dustImpactPrefabRef = dustImpactPrefab;
        metalImpactPrefabRef = metalImpactPrefab;
        projectileImpactPrefabRef = projectileImpactPrefab;

        if (objectPools == null) {
            objectPools = new Dictionary<GameObject, ObjectPool>(3);
            objectPools[dustImpactPrefab] = new ObjectPool(dustImpactPrefab, 10);
            objectPools[metalImpactPrefab] = new ObjectPool(metalImpactPrefab, 10);
            objectPools[projectileImpactPrefab] = new ObjectPool(projectileImpactPrefab, 10);
        }
    }

    public struct ProjectileImpactEvent {
        public Entity a;
        public Entity b;
        public float3 normal;
        public float3 position;
    }

    public enum ImpactType {
        GROUND,
        METAL,
        PROJECTILE,
    }

    internal static void onImpact(
            ImpactType impactType,
            ImpactEffect? effectTagA,
            ImpactEffect? effectTagB,
            float3 position,
            float3 normal
    ) {
        if (math.isnan(position.x)) return;
        if (effectTagA.HasValue) {
            instantiateImpactEffect(effectTagA.Value.value, impactType, position, normal);
        }
        if (effectTagB.HasValue) {
            instantiateImpactEffect(effectTagB.Value.value, impactType, position, normal);
        }
    }

    private static void instantiateImpactEffect(
            ProjectileEffect effectType,
            ImpactType impactType,
            float3 position,
            float3 normal
    ) {
        var effectInstance = objectPools[getEffectForImpact(effectType, impactType)].getObject();
        effectInstance.transform.position = new Vector3(position.x, position.y, position.z);
        effectInstance.transform.rotation = Quaternion.LookRotation(normal);
        effectInstance.SetActive(true);
    }

    private static GameObject getEffectForImpact(ProjectileEffect effectType, ImpactType impactType) {
        switch (effectType) {
            case ProjectileEffect.BULLET: {
                    switch (impactType) {
                        case ImpactType.GROUND:
                            return ProjectileImpactManager.dustImpactPrefabRef;
                        case ImpactType.METAL:
                            return ProjectileImpactManager.metalImpactPrefabRef;
                        case ImpactType.PROJECTILE:
                            return ProjectileImpactManager.projectileImpactPrefabRef;
                        default:
                            throw new ArgumentException($"unhandled impact type {impactType}");
                    }
                }
            default:
                throw new ArgumentException($"unhandled effect type {effectType}");
        }
    }
}

[UpdateInGroup(typeof(LateSimulationSystemGroup))]
public class QueuedActionSystem : ComponentSystem {

    protected override void OnUpdate() {
        ProjectileImpactManager.ProjectileImpactEvent projectileEvent;
        while (ProjectileImpactManager.queuedProjectileEvents.TryDequeue(out projectileEvent)) {
            ProjectileImpactManager.ImpactType impactType;

            var exitEarly = false;
            if (!EntityManager.HasComponent<Translation>(projectileEvent.a)) {
                World.Active.EntityManager.DestroyEntity(projectileEvent.a);
                exitEarly = true;
            }
            if (!EntityManager.HasComponent<Translation>(projectileEvent.b)) {
                World.Active.EntityManager.DestroyEntity(projectileEvent.b);
                exitEarly = true;
            }
            if (exitEarly) continue;

            var aIsProjectile = EntityManager.HasComponent<Projectile>(projectileEvent.a);
            var bIsProjectile = EntityManager.HasComponent<Projectile>(projectileEvent.b);
            if (aIsProjectile && bIsProjectile) {
                impactType = ProjectileImpactManager.ImpactType.PROJECTILE;

            } else if (aIsProjectile
                && (EntityManager.HasComponent<BTeamTag>(projectileEvent.b)
                    || EntityManager.HasComponent<ATeamTag>(projectileEvent.b))
            ) {
                impactType = ProjectileImpactManager.ImpactType.METAL;

            } else if (bIsProjectile
                && (EntityManager.HasComponent<BTeamTag>(projectileEvent.a)
                    || EntityManager.HasComponent<ATeamTag>(projectileEvent.a))
            ) {
                impactType = ProjectileImpactManager.ImpactType.METAL;

            } else {
                impactType = ProjectileImpactManager.ImpactType.GROUND;
            }

            ImpactEffect? aEffectTag = null;
            if (EntityManager.HasComponent<ImpactEffect>(projectileEvent.a)) {
                aEffectTag = EntityManager.GetComponentData<ImpactEffect>(projectileEvent.a);
            }
            ImpactEffect? bEffectTag = null;
            if (EntityManager.HasComponent<ImpactEffect>(projectileEvent.b)) {
                bEffectTag = EntityManager.GetComponentData<ImpactEffect>(projectileEvent.b);
            }
            ProjectileImpactManager.onImpact(
                impactType,
                aEffectTag,
                bEffectTag,
                projectileEvent.position,
                projectileEvent.normal
            );

            // TODO:
            // reduce HP of target if it has a HP object
            // destroy target if it had HP and it's now <= 0 
            // if target destroyed and projectile firer "HasTarget", remove that tag from firer
            // if target is ground create dust piff effect
            // else create sparks piff effect

            if (aIsProjectile) {
                World.Active.EntityManager.DestroyEntity(projectileEvent.a);
            }
            if (bIsProjectile) {
                World.Active.EntityManager.DestroyEntity(projectileEvent.b);
            }
        }
    }

    protected override void OnDestroy() {
        ProjectileImpactManager.queuedProjectileEvents.Dispose();
    }
}
