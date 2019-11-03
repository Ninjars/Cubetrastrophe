using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

public class TargetSpawner : MonoBehaviour {
    [SerializeField] private GameObject targetPrefab;
    [SerializeField] private int maxTargetCount;
    [SerializeField] private float range;
    private EntityManager entityManager;
    private Entity targetEntity;

    void Start() {
        entityManager = World.Active.EntityManager;

        targetEntity = GameObjectConversionUtility.ConvertGameObjectHierarchy(targetPrefab, World.Active);
        entityManager.AddComponentData(targetEntity, new BTeamTag {});

        for (int i = 0; i < maxTargetCount; i++) {
            spawnTargetEntity();
        }
    }

    private void spawnTargetEntity() {
        var instance = entityManager.Instantiate(targetEntity);
        entityManager.SetComponentData(
            instance,
            new Translation { Value = transform.TransformPoint(UnityEngine.Random.Range(-range, range), 1f, UnityEngine.Random.Range(-range, range)) }
        );
    }
}