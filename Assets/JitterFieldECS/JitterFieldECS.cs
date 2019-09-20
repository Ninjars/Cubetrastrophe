﻿using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class JitterFieldECS : MonoBehaviour {

    public GameObject cubePrefab;
    public int gridSize = 100;
    public GameObject spherePrefab;
    public int sphereCount = 50;

    void Start() {
        // Create entity prefab from the game object hierarchy once
        var cube = GameObjectConversionUtility.ConvertGameObjectHierarchy(cubePrefab, World.Active);
        var sphere = GameObjectConversionUtility.ConvertGameObjectHierarchy(spherePrefab, World.Active);
        var entityManager = World.Active.EntityManager;

        var gridOffset = gridSize / 2;
        for (var x = 0; x < gridSize; x++) {
            for (var y = 0; y < gridSize; y++) {
                var instance = entityManager.Instantiate(cube);
                var position = transform.TransformPoint(new float3(x - gridOffset, 0, y));
                entityManager.SetComponentData(instance, new Translation { Value = position });
                entityManager.AddComponentData(instance, new FieldCubeTag {});
            }
        }

        for (var i = 0; i < sphereCount; i++) {
            var instance = entityManager.Instantiate(sphere);
            var position = transform.TransformPoint(new float3(0 + UnityEngine.Random.value, 2 + i * 0.6f, gridOffset + UnityEngine.Random.value));
            entityManager.SetComponentData(instance, new Translation { Value = position });
        }
    }

    void Update() {

    }
}
