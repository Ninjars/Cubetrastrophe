using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

public class ProjectileImpactManager : MonoBehaviour {

    struct ProjectileMiss : IComponentData {}

    public static NativeQueue<ProjectileImpactEvent> queuedProjectileEvents;

    public GameObject dustImpactPrefab;
    public GameObject metalImpactPrefab;
    public GameObject projectileImpactPrefab;

    public static GameObject dustImpactPrefabRef { get; private set; }
    public static GameObject metalImpactPrefabRef { get; private set; }
    public static GameObject projectileImpactPrefabRef { get; private set; }

    private void Awake() {
        queuedProjectileEvents = new NativeQueue<ProjectileImpactEvent>(Allocator.Persistent);
        dustImpactPrefabRef = dustImpactPrefab;
        metalImpactPrefabRef = metalImpactPrefab;
        projectileImpactPrefabRef = projectileImpactPrefab;
    }

    public struct ProjectileImpactEvent {
        public Entity projectile;
        public Entity other;
        public float3 normal;
        public float3 position;
    }

    public enum ImpactType {
        GROUND,
        METAL,
        PROJECTILE,
    }

    internal static void onImpact(ImpactType impactType, float3 position, float3 normal) {
        if (math.isnan(position.x)) return;
        GameObject prefab;
        switch (impactType) {
            case ImpactType.GROUND:
                prefab = ProjectileImpactManager.dustImpactPrefabRef;
                break;
            case ImpactType.METAL:
                prefab = ProjectileImpactManager.metalImpactPrefabRef;
                break;
            case ImpactType.PROJECTILE:
                prefab = ProjectileImpactManager.projectileImpactPrefabRef;
                break;
            default:
                prefab = ProjectileImpactManager.metalImpactPrefabRef;
                break;
        }
        var effectInstance = GameObject.Instantiate(prefab);
        effectInstance.transform.position = new Vector3(position.x, position.y, position.z);
        effectInstance.transform.rotation = Quaternion.LookRotation(normal);
    }
}

[UpdateInGroup(typeof(LateSimulationSystemGroup))]
public class QueuedActionSystem : ComponentSystem {

    protected override void OnUpdate() {
        ProjectileImpactManager.ProjectileImpactEvent projectileEvent;
        var destroyOther = false;
        while (ProjectileImpactManager.queuedProjectileEvents.TryDequeue(out projectileEvent)) {
            var projectileEntity = projectileEvent.projectile;

            if (!EntityManager.HasComponent<Translation>(projectileEntity)) {
                // I guess it's already dead?
                World.Active.EntityManager.DestroyEntity(projectileEntity);
                continue;
            }

            ProjectileImpactManager.ImpactType impactType;
            if (EntityManager.HasComponent<Projectile>(projectileEvent.other)) {
                impactType = ProjectileImpactManager.ImpactType.PROJECTILE;
                destroyOther = true;
            } else if (EntityManager.HasComponent<BTeamTag>(projectileEvent.other) || EntityManager.HasComponent<ATeamTag>(projectileEvent.other)) {
                impactType = ProjectileImpactManager.ImpactType.METAL;
            } else {
                impactType = ProjectileImpactManager.ImpactType.GROUND;
            }

            float3 position = projectileEvent.position;
            float3 normal = projectileEvent.normal;
            ProjectileImpactManager.onImpact(impactType, position, normal);

            // TODO:
            // reduce HP of target if it has a HP object
            // destroy target if it had HP and it's now <= 0 
            // if target destroyed and projectile firer "HasTarget", remove that tag from firer
            // if target is ground create dust piff effect
            // else create sparks piff effect

            World.Active.EntityManager.DestroyEntity(projectileEntity);
            if (destroyOther) {
                World.Active.EntityManager.DestroyEntity(projectileEvent.other);
            }
        }
    }

    protected override void OnDestroy() {
        ProjectileImpactManager.queuedProjectileEvents.Dispose();
    }
}
