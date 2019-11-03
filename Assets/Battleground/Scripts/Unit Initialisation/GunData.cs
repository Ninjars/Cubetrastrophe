using Unity.Entities;
using UnityEngine;

[CreateAssetMenu(fileName = "Turret", menuName = "Scriptables/Turret")]
public class TurretDefinition : ScriptableObject {
    public GameObject basePrefab;
    public GunDefinition gun;
    public Vector3 gunOffset;
}

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

[CreateAssetMenu(fileName = "Projectile", menuName = "Scriptables/Projectile")]
public class ProjectileDefinition : ScriptableObject {
    public GameObject prefab;
    public ProjectileEffect effect;
}

public enum ProjectileEffect {
    BULLET,
}

static class ProjectileEffectExtensions {
    public static ImpactEffect toComponent(this ProjectileEffect effect) {
        return new ImpactEffect { value = ProjectileEffect.BULLET };
    }
}

public struct ImpactEffect : IComponentData {
    public ProjectileEffect value;
    public void AssignToEntity(EntityManager manager, Entity entity) {
        manager.AddComponentData(entity, this);
    }
}