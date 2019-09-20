using System.Collections;
using System.Collections.Generic;
using Unity.Jobs;
using UnityEngine;

public class JitterField : MonoBehaviour {

    public JitterCube cubePrefab;
    public int gridSize = 100;
    private JitterCube[,] cubes;
    private PerlinProvider noiseMachine;

    void Start() {
        cubes = new JitterCube[gridSize, gridSize];
        noiseMachine = new PerlinProvider(0.01f, 0, 0.5f, 2, 1);

        var gridOffset = gridSize / 2;
        for (var x = 0; x < gridSize; x++) {
            for (var y = 0; y < gridSize; y++) {
                var cube = GameObject.Instantiate(cubePrefab);
                cube.transform.position = new Vector3(x - gridOffset, 0, y);
                cube.name = $"{x}, {y}";
                cubes[x, y] = cube;
            }
        }
    }

    void Update() {
        updateSingleThreaded();
    }

    private void updateSingleThreaded() {
        noiseMachine.updateOffset(0.1f, 0.1f);
        for (var x = 0; x < gridSize; x++) {
            for (var y = 0; y < gridSize; y++) {
                var cube = cubes[x, y];
                cube.energy = noiseMachine.get(x, y);
            }
        }
    }

    private void updateWithJob() {
        
    }
}
