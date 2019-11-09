using UnityEngine;

[CreateAssetMenu(fileName = "Turret", menuName = "Scriptables/Turret")]
public class TurretDefinition : ScriptableObject {
    public GameObject basePrefab;
    public GunDefinition gun;
    public Vector3 gunOffset;
}