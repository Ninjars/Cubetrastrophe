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
        entityManager.SetComponentData(
            entityManager.Instantiate(baseEntity),
            new Translation {
                Value = transform.TransformPoint(0, 0, 0)
            }
        );

        Entity projectileEntity = GameObjectConversionUtility.ConvertGameObjectHierarchy(projectilePrefab, World.Active);
        entityManager.AddComponentData(projectileEntity, new Projectile {});

        Entity turretEntity = GameObjectConversionUtility.ConvertGameObjectHierarchy(turretPrefab, World.Active);
        var instance = entityManager.Instantiate(turretEntity);
        entityManager.SetComponentData(
            instance,
            new Translation { Value = transform.TransformPoint(0, 0, 0) }
        );
        entityManager.AddComponentData(instance, new GunData {
            projectileEntity = projectileEntity,
            projectileOffset = new float3(1.3f, 1.33f, 0),
            projectileVelocity = 10f,
            reloadInterval = 1.5f,
            fireInterval = 0.5f,
            shotsPerReload = 3,
        });
        entityManager.AddComponentData(instance, new GunState {
            currentFireInterval = 0,
            currentReloadInterval = 0,
            shotsRemaining = 3,
        });
    }
}
