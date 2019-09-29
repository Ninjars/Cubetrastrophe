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

    public static GameObject dustImpactPrefabRef { get; private set; }
    public static GameObject metalImpactPrefabRef { get; private set; }

    private void Awake() {
        queuedProjectileEvents = new NativeQueue<ProjectileImpactEvent>(Allocator.Persistent);
        dustImpactPrefabRef = dustImpactPrefab;
        metalImpactPrefabRef = metalImpactPrefab;
    }

    public struct ProjectileImpactEvent {
        public Entity projectile;
        public Entity other;
        public float3 normal;
        public float3 position;
    }

    public enum ImpactType {
        GROUND,
        METAL
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
        // EntityQuery entityQuery = GetEntityQuery(typeof(Zombie));
        // int entityCount = entityQuery.CalculateLength();

        ProjectileImpactManager.ProjectileImpactEvent projectileEvent;
        while (ProjectileImpactManager.queuedProjectileEvents.TryDequeue(out projectileEvent)) {
            var projectileEntity = projectileEvent.projectile;

            if (!EntityManager.HasComponent<Translation>(projectileEntity)) {
                // I guess it's already dead?
                World.Active.EntityManager.DestroyEntity(projectileEntity);
                continue;
            }

            ProjectileImpactManager.ImpactType impactType;
            if (EntityManager.HasComponent<TargetTag>(projectileEvent.other) || EntityManager.HasComponent<Turret>(projectileEvent.other)) {
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

            // if (EntityManager.Exists(projectileEvent.marineEntity) && EntityManager.Exists(projectileEvent.zombieEntity)) {
            //     float3 marinePosition = EntityManager.GetComponentData<Translation>(projectileEvent.marineEntity).Value;
            //     float3 zombiePosition = EntityManager.GetComponentData<Translation>(projectileEvent.zombieEntity).Value;
            //     float3 marineToZombieDir = math.normalize(zombiePosition - marinePosition);

            //     bool bonusEffects = (entityCount < 400 || UnityEngine.Random.Range(0, 100) < 60);
            //     if (bonusEffects) {
            //         WeaponTracer.Create(marinePosition + marineToZombieDir * 10f, (Vector3)zombiePosition + UtilsClass.GetRandomDir() * UnityEngine.Random.Range(0, 20f));
            //         Shoot_Flash.AddFlash(marinePosition + marineToZombieDir * 14f);
            //         Blood_Handler.SpawnBlood(2, zombiePosition, marineToZombieDir);
            //         UtilsClass.ShakeCamera(TestECS.GetCameraShakeIntensity(), .05f);
            //         //Sound_Manager.PlaySound(Sound_Manager.Sound.Rifle_Fire, marinePosition);
            //     }

            //     Health zombieHealth = EntityManager.GetComponentData<Health>(projectileEvent.zombieEntity);
            //     zombieHealth.health -= projectileEvent.damageAmount;
            //     if (zombieHealth.health < 0) {
            //         // Zombie dead!
            //         FlyingBody.TryCreate(GameAssets.i.pfEnemyFlyingBody, zombiePosition, marineToZombieDir);
            //         EntityManager.DestroyEntity(projectileEvent.zombieEntity);
            //         EntityManager.RemoveComponent<HasTarget>(projectileEvent.marineEntity);
            //     } else {
            //         // Zombie still has health
            //         EntityManager.SetComponentData(projectileEvent.zombieEntity, zombieHealth);
            //     }
            // } else {
            //     if (EntityManager.Exists(projectileEvent.marineEntity) && !EntityManager.Exists(projectileEvent.zombieEntity)) {
            //         // Marine exists but zombie is dead
            //         EntityManager.RemoveComponent<HasTarget>(projectileEvent.marineEntity);
            //     }
            // }
        }
    }

    protected override void OnDestroy() {
        ProjectileImpactManager.queuedProjectileEvents.Dispose();
    }
}
