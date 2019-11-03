using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class Attacker : MonoBehaviour {

    public GameObject bodyPrefab;
    public Turret turretPrefab;
    public List<TurretInfo> turretPositionInfo;
    public Team team;

    void Start() {
        var entityManager = World.Active.EntityManager;

        var bodyEntity = GameObjectConversionUtility.ConvertGameObjectHierarchy(bodyPrefab, World.Active);
        var baseInstance = entityManager.Instantiate(bodyEntity);
        entityManager.SetComponentData(
            baseInstance,
            new Translation { Value = transform.TransformPoint(0, 0, 0) }
        );
        entityManager.SetComponentData(
            baseInstance,
            new Rotation { Value = transform.rotation }
        );
        TeamTag tag = team.toComponent();
        tag.AssignToEntity(entityManager, baseInstance);

        var baseRotation = entityManager.GetComponentData<Rotation>(baseInstance);

        foreach (TurretInfo t in turretPositionInfo) {
            var turret = GameObject.Instantiate(turretPrefab);
            turret.selfInstantiate = false;
            turret.team = team;

            Entity turretEntity = turret.instantiate(baseRotation, t.position, quaternion.EulerXYZ(math.radians(t.facing.x), math.radians(t.facing.y), math.radians(t.facing.z)));
            entityManager.AddComponent(turretEntity, typeof(LocalToParent));
            entityManager.AddComponent(turretEntity, typeof(Parent));
            entityManager.SetComponentData<Parent>(turretEntity, new Parent { Value = baseInstance });
        }
    }
}

[Serializable]
public struct TurretInfo {
    public float3 position;
    public float3 facing;
}