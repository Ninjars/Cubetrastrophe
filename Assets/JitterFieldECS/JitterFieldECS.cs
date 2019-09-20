using System.Collections;
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
    private PerlinProvider noiseMachine;

    void Start() {
        noiseMachine = new PerlinProvider(0.01f, 0, 0.5f, 2, 1);

        // Create entity prefab from the game object hierarchy once
        var prefab = GameObjectConversionUtility.ConvertGameObjectHierarchy(cubePrefab, World.Active);
        var entityManager = World.Active.EntityManager;

        var gridOffset = gridSize / 2;
        for (var x = 0; x < gridSize; x++) {
            for (var y = 0; y < gridSize; y++) {
                var instance = entityManager.Instantiate(prefab);
                var position = transform.TransformPoint(new float3(x - gridOffset, 0, y));
                entityManager.SetComponentData(instance, new Translation { Value = position });
            }
        }
    }

    void Update() {

    }
}
