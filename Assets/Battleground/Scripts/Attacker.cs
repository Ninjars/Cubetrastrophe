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
                turret.transform.position = t.position;
                turret.transform.rotation = t.rotation;
                turret.selfInstantiate = false;
                turret.team = team;

                Entity turretEntity = turret.instantiate();

                // var aEntity = entityManager.CreateEntity(typeof(Attach));
                // entityManager.SetComponentData(aEntity, new Attach() { Parent = entityParent, Child = entity });
            }
        }
    }
}