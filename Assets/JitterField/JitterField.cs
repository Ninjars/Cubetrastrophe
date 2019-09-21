using UnityEngine;

public class JitterField : MonoBehaviour {

    public JitterCube cubePrefab;
    public int gridSize = 100;
    public GameObject spherePrefab;
    public int sphereCount = 50;
    private JitterCube[,] cubes;

    void Start() {
        cubes = new JitterCube[gridSize, gridSize];

        var gridOffset = gridSize / 2;
        for (var x = 0; x < gridSize; x++) {
            for (var y = 0; y < gridSize; y++) {
                var cube = GameObject.Instantiate(cubePrefab);
                cube.transform.position = new Vector3(x - gridOffset, 0, y);
                cube.name = $"{x}, {y}";
                cubes[x, y] = cube;
            }
        }

        for (var i = 0; i < sphereCount; i++) {
            var instance = GameObject.Instantiate(spherePrefab);
            instance.transform.position =new Vector3(UnityEngine.Random.value, 2 + i * 0.6f, gridOffset / 3 + UnityEngine.Random.value);
        }
    }
}
