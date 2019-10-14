﻿using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class Turret : MonoBehaviour {

    public GameObject basePrefab;
    public GameObject turretPrefab;
    public GameObject projectilePrefab;
    public GameObject muzzleEffectPrefab;
    public Team team;
    public bool selfInstantiate;
    
    public static GameObject muzzleEffectPrefabRef { get; private set; }

    void Start() {
        if (selfInstantiate) {
            instantiate();
        }
    }
    public Entity instantiate() {
        muzzleEffectPrefabRef = muzzleEffectPrefab;
        var entityManager = World.Active.EntityManager;

        var baseEntity = GameObjectConversionUtility.ConvertGameObjectHierarchy(basePrefab, World.Active);
        var baseInstance = entityManager.Instantiate(baseEntity); 
        entityManager.SetComponentData(
            baseInstance,
            new Translation { Value = transform.position }
        );
        entityManager.SetComponentData(
            baseInstance,
            new Rotation { Value = transform.rotation }
        );

        Entity projectileEntity = GameObjectConversionUtility.ConvertGameObjectHierarchy(projectilePrefab, World.Active);
        Entity turretEntity = GameObjectConversionUtility.ConvertGameObjectHierarchy(turretPrefab, World.Active);

        TeamTag tag = team.toComponent();
        tag.AssignToEntity(entityManager, turretEntity);

        var instance = entityManager.Instantiate(turretEntity);
        entityManager.AddComponent(instance, typeof(LocalToParent));
        entityManager.AddComponent(instance, typeof(Parent));
        entityManager.SetComponentData<Parent>(
            instance, 
            new Parent { Value = baseInstance }
        );

        entityManager.SetComponentData(
            instance,
            new Translation { Value = math.mul(transform.rotation, new float3(0, 0.815f, 0)) }
        );
        entityManager.SetComponentData(
            instance,
            new Rotation { Value = transform.rotation }
        );
        entityManager.AddComponentData(instance, new GunData {
            projectileEntity = projectileEntity,
            projectileOffset = new float3(0, 0.52f, 1.3f),
            projectileVelocity = 100f,
            reloadInterval = 0.5f,
            fireInterval = 0.1f,
            shotsPerReload = 3,
            shotDeviation = math.radians(5f),
            maximumPitchDelta = math.radians(21f),
            rotationSpeed = 1f,
            pitchSpeed = 5f,
            neutralRotation = quaternion.identity,
        });
        
        entityManager.AddComponentData(instance, new GunState {
            currentFireInterval = 0,
            currentReloadInterval = 0,
            shotsRemaining = 4,
            currentPitch = math.PI/2f,
            targetPitchDelta = math.PI/2f,
        });

        return baseInstance;
    }

    internal static void onShotFired(float3 position, quaternion rotationQuaternion) {
        if (math.isnan(position.x)) return;
        var rotation = rotationQuaternion.value;
        if (math.isnan(rotation.x)) return;
        var effectInstance = GameObject.Instantiate(muzzleEffectPrefabRef);
        effectInstance.transform.position = new Vector3(position.x, position.y, position.z);
        effectInstance.transform.rotation = new Quaternion(rotation.x, rotation.y, rotation.z, rotation.w);
    }
}
