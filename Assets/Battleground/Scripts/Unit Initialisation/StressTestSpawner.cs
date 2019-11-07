using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class StressTestSpawner : MonoBehaviour {
    [SerializeField] private TurretDefinition turret;
    [SerializeField] private int count;
    [SerializeField] private float spacing;
    [SerializeField] private Team team;
    private EntityManager entityManager;
    private Entity entity;

    void Start() {
        float offset = spacing * -count / 2;
        for (int x = 0; x < count; x++) {
            for (int y = 0; y < count; y++) {
                spawnTargetEntity(offset + spacing * x, spacing * y, -spacing * y);

            }
        }
    }

    private void spawnTargetEntity(float x, float y, float z) {
        var info = new TurretInfo();
        info.definition = turret;
        info.facing = transform.forward;
        info.position = new float3(x, y, z);
        TurretInstantiator.instantiate(team, info, null);
    }
}
