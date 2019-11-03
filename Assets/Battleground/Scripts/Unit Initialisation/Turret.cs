using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class Turret : MonoBehaviour {
    public TurretDefinition definition;
    public Team team;
    public bool selfInstantiate;

    private static Dictionary<int, ObjectPool> muzzleEffectPools = new Dictionary<int, ObjectPool>();
    private static Dictionary<GameObject, Entity> entities = new Dictionary<GameObject, Entity>();

    void Awake() {
        int muzzelEffectId = definition.gun.muzzleEffectPrefab.GetHashCode();
        if (!muzzleEffectPools.ContainsKey(muzzelEffectId)) {
            muzzleEffectPools[muzzelEffectId] = new ObjectPool(definition.gun.muzzleEffectPrefab, 10);
        }

        var entityManager = World.Active.EntityManager;
        if (!entities.ContainsKey(definition.basePrefab)) {
            entities[definition.basePrefab] = GameObjectConversionUtility.ConvertGameObjectHierarchy(definition.basePrefab, World.Active);
        }
        if (!entities.ContainsKey(definition.gun.prefab)) {
            entities[definition.gun.prefab] = GameObjectConversionUtility.ConvertGameObjectHierarchy(definition.gun.prefab, World.Active);
        }
        if (!entities.ContainsKey(definition.gun.projectile.prefab)) {
            var projectile = GameObjectConversionUtility.ConvertGameObjectHierarchy(definition.gun.projectile.prefab, World.Active);
            entityManager.AddComponentData(projectile, new ImpactEffect { value = definition.gun.projectile.effect });
            entities[definition.gun.projectile.prefab] = projectile;
        }
    }

    void Start() {
        if (selfInstantiate) {
            instantiate(null, transform.position, transform.rotation);
        }
    }

    public Entity instantiate(Rotation? parentRotation, float3 position, quaternion rotation) {
        var entityManager = World.Active.EntityManager;

        var baseInstance = entityManager.Instantiate(entities[definition.basePrefab]);
        entityManager.SetComponentData(
            baseInstance,
            new Translation { Value = position }
        );
        entityManager.SetComponentData(
            baseInstance,
            new Rotation { Value = rotation }
        );

        var gun = definition.gun;
        var instance = entityManager.Instantiate(entities[gun.prefab]);
        TeamTag tag = team.toComponent();
        tag.AssignToEntity(entityManager, instance);

        entityManager.AddComponent(instance, typeof(LocalToParent));
        entityManager.AddComponent(instance, typeof(Parent));
        entityManager.SetComponentData<Parent>(
            instance,
            new Parent { Value = baseInstance }
        );

        entityManager.SetComponentData(
            instance,
            new Translation { Value = definition.gunOffset }
        );
        entityManager.SetComponentData(
            instance,
            new Rotation { Value = rotation }
        );
        entityManager.AddComponentData(instance, new GunData {
            projectileEntity = entities[gun.projectile.prefab],
            projectileOffset = gun.barrelOffset,
            muzzleFlashEffect = gun.muzzleEffectPrefab.GetHashCode(),
            projectileVelocity = gun.muzzleVelocity,
            reloadInterval = gun.reloadInterval,
            fireInterval = gun.fireInterval,
            shotsPerReload = gun.shotsPerReload,
            shotDeviation = math.radians(gun.shotDeviationDegrees),
            maximumPitchDelta = math.radians(gun.maximumPitchDeltaDegrees),
            rotationSpeed = 1f,
            pitchSpeed = 5f,
            neutralRotation = rotation,
            parentRotation = parentRotation,
        });

        entityManager.AddComponentData(instance, new GunState {
            currentFireInterval = 0,
            currentReloadInterval = 0,
            shotsRemaining = 4,
            currentPitch = math.PI / 2f,
            targetPitchDelta = math.PI / 2f,
        });

        return baseInstance;
    }

    internal static void onShotFired(int effect, float3 position, quaternion rotationQuaternion) {
        if (math.isnan(position.x)) return;
        var rotation = rotationQuaternion.value;
        if (math.isnan(rotation.x)) return;
        var effectInstance = muzzleEffectPools[effect].getObject();
        effectInstance.transform.position = new Vector3(position.x, position.y, position.z);
        effectInstance.transform.rotation = new Quaternion(rotation.x, rotation.y, rotation.z, rotation.w);
        effectInstance.SetActive(true);
    }
}
