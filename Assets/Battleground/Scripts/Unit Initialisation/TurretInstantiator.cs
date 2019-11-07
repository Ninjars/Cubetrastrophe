
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

class TurretInstantiator {
    
    private static Dictionary<int, ObjectPool> muzzleEffectPools = new Dictionary<int, ObjectPool>();
    private static Dictionary<int, Entity> entities = new Dictionary<int, Entity>();

    private static void instantiateData(TurretDefinition definition) {
        int muzzelEffectId = definition.gun.muzzleEffectPrefab.GetHashCode();
        if (!muzzleEffectPools.ContainsKey(muzzelEffectId)) {
            muzzleEffectPools[muzzelEffectId] = new ObjectPool(definition.gun.muzzleEffectPrefab, 10);
        }
        
        int projectileId = definition.gun.projectile.prefab.GetHashCode();
        if (!entities.ContainsKey(projectileId)) {
            var projectile = GameObjectConversionUtility.ConvertGameObjectHierarchy(definition.gun.projectile.prefab, World.Active);
            World.Active.EntityManager.AddComponentData(projectile, new ImpactEffect { value = definition.gun.projectile.effect });
            entities[projectileId] = projectile;
        }

        convertPrefabIfNeeded(definition.basePrefab);
        convertPrefabIfNeeded(definition.gun.prefab);
    }

    private static void convertPrefabIfNeeded(GameObject prefab) {
        int id = prefab.GetHashCode();
        if (!entities.ContainsKey(id)) {
            entities[id] = GameObjectConversionUtility.ConvertGameObjectHierarchy(prefab, World.Active);
        }
    }

    public static Entity instantiate(Team team, TurretInfo info, Rotation? parentRotation) {
        var entityManager = World.Active.EntityManager;
        var definition = info.definition;
        instantiateData(definition);
        
        var baseInstance = entityManager.Instantiate(entities[definition.basePrefab.GetHashCode()]);
        entityManager.SetComponentData(
            baseInstance,
            new Translation { Value = info.position }
        );

        var rotation = quaternion.EulerXYZ(math.radians(info.facing.x), math.radians(info.facing.y), math.radians(info.facing.z));
        entityManager.SetComponentData(
            baseInstance,
            new Rotation { Value = rotation }
        );

        var gun = definition.gun;
        var instance = entityManager.Instantiate(entities[gun.prefab.GetHashCode()]);
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
            projectileEntity = entities[gun.projectile.prefab.GetHashCode()],
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

    public static void onShotFired(int effect, float3 position, quaternion rotationQuaternion) {
        if (math.isnan(position.x)) return;
        var rotation = rotationQuaternion.value;
        if (math.isnan(rotation.x)) return;
        var effectInstance = muzzleEffectPools[effect].getObject();
        effectInstance.transform.position = new Vector3(position.x, position.y, position.z);
        effectInstance.transform.rotation = new Quaternion(rotation.x, rotation.y, rotation.z, rotation.w);
        effectInstance.SetActive(true);
    }
}
