using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[CreateAssetMenu(fileName = "Unit", menuName = "Scriptables/Unit")]
public class UnitDefinition : ScriptableObject {
    public GameObject bodyPrefab;
    public Turret turretPrefab;
    public List<TurretInfo> turretPositionInfo;
    public Team team = Team.TEAM_B;
}

[Serializable]
public struct TurretInfo {
    public float3 position;
    public float3 facing;
}

public class UnitSpawner : MonoBehaviour {

    public UnitDefinition unitDef;
    private Entity templateEntity;

    void Start() {
        templateEntity = GameObjectConversionUtility.ConvertGameObjectHierarchy(unitDef.bodyPrefab, World.Active);
        instantiate(templateEntity);
    }

    private Entity instantiate(Entity template) {
        var entityManager = World.Active.EntityManager;
        var entity = entityManager.Instantiate(template);
        entityManager.SetComponentData(
            entity,
            new Translation { Value = transform.TransformPoint(0, 0, 0) }
        );
        entityManager.SetComponentData(
            entity,
            new Rotation { Value = transform.rotation }
        );
        TeamTag tag = unitDef.team.toComponent();
        tag.AssignToEntity(entityManager, entity);

        var baseRotation = entityManager.GetComponentData<Rotation>(entity);

        foreach (TurretInfo t in unitDef.turretPositionInfo) {
            var turret = GameObject.Instantiate(unitDef.turretPrefab);
            turret.selfInstantiate = false;
            turret.team = unitDef.team;

            Entity turretEntity = turret.instantiate(baseRotation, t.position, quaternion.EulerXYZ(math.radians(t.facing.x), math.radians(t.facing.y), math.radians(t.facing.z)));
            entityManager.AddComponent(turretEntity, typeof(LocalToParent));
            entityManager.AddComponent(turretEntity, typeof(Parent));
            entityManager.SetComponentData<Parent>(turretEntity, new Parent { Value = entity });
        }

        return entity;
    }
}
