using UnityEngine;

[CreateAssetMenu(fileName = "Projectile", menuName = "Scriptables/Projectile")]
public class ProjectileDefinition : ScriptableObject {
    public GameObject prefab;
    public ProjectileEffect effect;
}
