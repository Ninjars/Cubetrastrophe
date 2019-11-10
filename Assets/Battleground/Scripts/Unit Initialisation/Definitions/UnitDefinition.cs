using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

[CreateAssetMenu(fileName = "Unit", menuName = "Scriptables/Unit")]
public class UnitDefinition : ScriptableObject {
    public GameObject bodyPrefab;
    public List<TurretInfo> turretPositionInfo;
    public Vector3 initialVelocity;
    public Team team;
}

[Serializable]
public struct TurretInfo {
    public TurretDefinition definition;
    public float3 position;
    public float3 facing;
}
