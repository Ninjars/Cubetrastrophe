using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class ObjectSpawnerECS : MonoBehaviour {

    public GameObject prefab;

    public float spawnInterval;

    private float spawnTimer;
    private Entity entity;

    void Start() {
        entity = GameObjectConversionUtility.ConvertGameObjectHierarchy(prefab, World.Active);
    }

    void FixedUpdate() {
        spawnTimer += Time.fixedDeltaTime;
        if (spawnTimer > spawnInterval) {
            var entityManager = World.Active.EntityManager;
            var instance = entityManager.Instantiate(entity);
            var position = transform.TransformPoint(new float3(UnityEngine.Random.value, 20, UnityEngine.Random.value));

            entityManager.SetComponentData(instance, new Translation { Value = position });
            spawnTimer = 0;
        }
    }
}