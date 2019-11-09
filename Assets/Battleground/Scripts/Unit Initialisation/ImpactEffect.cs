using Unity.Entities;

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