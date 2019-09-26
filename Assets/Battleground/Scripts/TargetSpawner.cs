using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

public class TargetSpawner : MonoBehaviour {
    [SerializeField] private GameObject targetPrefab;
    [SerializeField] private int maxTargetCount;
    private EntityManager entityManager;
    private Entity targetEntity;

    void Start() {
        entityManager = World.Active.EntityManager;

        targetEntity = GameObjectConversionUtility.ConvertGameObjectHierarchy(targetPrefab, World.Active);
        entityManager.AddComponentData(targetEntity, new TargetTag {});

        for (int i = 0; i < maxTargetCount; i++) {
            spawnTargetEntity();
        }
    }

    private void spawnTargetEntity() {
        var instance = entityManager.Instantiate(targetEntity);
        entityManager.SetComponentData(
            instance,
            new Translation { Value = transform.TransformPoint(UnityEngine.Random.Range(-30, 30), 1f, UnityEngine.Random.Range(-30, 30)) }
        );
    }
}