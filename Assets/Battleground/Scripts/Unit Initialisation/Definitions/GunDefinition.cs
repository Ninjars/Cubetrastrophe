using UnityEngine;

[CreateAssetMenu(fileName = "Gun", menuName = "Scriptables/Gun")]
public class GunDefinition : ScriptableObject {
    public GameObject prefab;
    public GameObject muzzleEffectPrefab;
    public ProjectileDefinition projectile;

    public Vector3 barrelOffset;
    public float muzzleVelocity;
    public float reloadInterval;
    public float fireInterval;
    public int shotsPerReload;
    public float shotDeviationDegrees;
    public float maximumPitchDeltaDegrees;
    public float rotationSpeed;
    public float pitchSpeed;
}