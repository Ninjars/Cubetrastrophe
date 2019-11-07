using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

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

        foreach (TurretInfo turret in unitDef.turretPositionInfo) {
            Entity turretEntity = TurretInstantiator.instantiate(unitDef.team, turret, baseRotation);
            entityManager.AddComponent(turretEntity, typeof(LocalToParent));
            entityManager.AddComponent(turretEntity, typeof(Parent));
            entityManager.SetComponentData<Parent>(turretEntity, new Parent { Value = entity });
        }

        return entity;
    }
}
