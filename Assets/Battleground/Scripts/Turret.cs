using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

public class Turret : MonoBehaviour {

    public GameObject basePrefab;
    public GameObject turretPrefab;
    public GameObject projectilePrefab;

    void Start() {
        var entityManager = World.Active.EntityManager;

        var baseEntity = GameObjectConversionUtility.ConvertGameObjectHierarchy(basePrefab, World.Active);
        var baseInstance = entityManager.Instantiate(baseEntity); 
        entityManager.SetComponentData(
            baseInstance,
            new Translation { Value = transform.TransformPoint(0, 0, 0) }
        );
        entityManager.SetComponentData(
            baseInstance,
            new Rotation { Value = transform.rotation }
        );

        Entity projectileEntity = GameObjectConversionUtility.ConvertGameObjectHierarchy(projectilePrefab, World.Active);
        entityManager.AddComponentData(projectileEntity, new Projectile { });

        Entity turretEntity = GameObjectConversionUtility.ConvertGameObjectHierarchy(turretPrefab, World.Active);
        entityManager.AddComponentData(turretEntity, new DefenderTag { });

        var instance = entityManager.Instantiate(turretEntity);
        entityManager.SetComponentData(
            instance,
            new Translation { Value = new float3(transform.position) + math.mul(transform.rotation, new float3(0, 0.815f, 0)) }
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
            neutralRotation = transform.rotation,
            localRotationAxis = transform.up,
            localPitchAxis = transform.right,
        });
        
        var axisAngles = MathUtils.axisAngles(transform.rotation);
        entityManager.AddComponentData(instance, new GunState {
            currentFireInterval = 0,
            currentReloadInterval = 0,
            shotsRemaining = 4,
        });
    }
}
