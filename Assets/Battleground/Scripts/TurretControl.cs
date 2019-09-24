using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using System.Linq;

public class TurretControl : MonoBehaviour {

    public GameObject projectilePrefab;

    public float fireInterval;
    public float3 projectileOffset = new float3(.2f, 0, 0);

    private float fireTimer;
    private Entity entity;
    private EntityManager entityManager;
    private Transform[] firePoints;

    void Start() {
        Debug.Log("started");
        entityManager = World.Active.EntityManager;
        entity = GameObjectConversionUtility.ConvertGameObjectHierarchy(projectilePrefab, World.Active);
        firePoints = findAllChildrenWithTag("spawn").ToArray();
        Debug.Log($"fire point count: {firePoints.Count()}");
    }

    public List<Transform> findAllChildrenWithTag(string tag) {
        var objects = new List<Transform>();
        Transform parent = transform;
        findAllChildrenWithTag(parent, tag, objects);
        return objects;
    }

    public void findAllChildrenWithTag(Transform parent, string tag, List<Transform> objects) {
        for (int i = 0; i < parent.childCount; i++) {
            Transform child = parent.GetChild(i);
            if (child.tag == tag) {
                objects.Add(child);
            }
            if (child.childCount > 0) {
                findAllChildrenWithTag(child, tag, objects);
            }
        }
    }

    // Update is called once per frame
    void FixedUpdate() {
        Debug.Log("FixedUpdate");
        fireTimer += Time.fixedDeltaTime;
        if (fireTimer > fireInterval) {
            foreach (var item in firePoints) {
                entityManager.SetComponentData(
                    entityManager.Instantiate(entity),
                    new Translation {
                        Value = item.TransformPoint(projectileOffset)
                    }
                );
            }
            fireTimer = 0;
        }
    }
}
