using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class ObjectSpawnerECS : MonoBehaviour {

    public GameObject prefab;

    public float spawnInterval;

    private float spawnTimer;
    private Entity entity;
    private EntityManager entityManager;

    void Start() {
        entity = GameObjectConversionUtility.ConvertGameObjectHierarchy(prefab, World.Active);
        entityManager = World.Active.EntityManager;
    }

    void FixedUpdate() {
        spawnTimer += Time.fixedDeltaTime;
        if (spawnTimer > spawnInterval) {
            entityManager.SetComponentData(
                entityManager.Instantiate(entity), 
                new Translation { 
                    Value = transform.TransformPoint(new float3(UnityEngine.Random.value, 0, UnityEngine.Random.value))
                }
            );
            spawnTimer = 0;
        }
    }
}