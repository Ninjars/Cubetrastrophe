using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class Attacker : MonoBehaviour {

    public GameObject bodyPrefab;
    public Turret turretPrefab;
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

        foreach (Transform t in transform) {
            if (t.name == "Gun") {
                var turret = GameObject.Instantiate(turretPrefab);
                turret.transform.parent = transform;
                turret.selfInstantiate = false;
                turret.team = team;

                Entity turretEntity = turret.instantiate(t.position - transform.position, t.rotation);
                entityManager.AddComponent(turretEntity, typeof(LocalToParent));
                entityManager.AddComponent(turretEntity, typeof(Parent));
                entityManager.SetComponentData<Parent>(turretEntity, new Parent { Value = baseInstance });
            }
        }
    }
}