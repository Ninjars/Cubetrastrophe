using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Physics;
using Unity.Physics.Systems;
using UnityEngine;
using static ProjectileImpactManager;

struct CollisionJob : ICollisionEventsJob {
    [ReadOnly] internal ComponentDataFromEntity<Projectile> projectiles;
    public NativeQueue<ProjectileImpactEvent>.ParallelWriter queuedActions;

    public void Execute(CollisionEvent ev) {
        Entity a = ev.Entities.EntityA;
        Entity b = ev.Entities.EntityB;
        if (projectiles.Exists(a)) {
            enqueueCollision(ev, a, b);
        } else if (projectiles.Exists(b)) {
            enqueueCollision(ev, b, a);
        }
    }

    private void enqueueCollision(CollisionEvent collision, Entity projectile, Entity other) {
        queuedActions.Enqueue(new ProjectileImpactEvent {
            collisionEvent = collision,
            projectile = projectile,
            other = other
        });
    }
}

[UpdateAfter(typeof(StepPhysicsWorld))]
[UpdateBefore(typeof(EndFramePhysicsSystem))]
public class CollisionSystem : JobComponentSystem {

    BuildPhysicsWorld buildPhysicsWorldSystem;
    StepPhysicsWorld stepPhysicsWorld;

    protected override void OnCreate() {
        buildPhysicsWorldSystem = World.GetOrCreateSystem<BuildPhysicsWorld>();
        stepPhysicsWorld = World.GetOrCreateSystem<StepPhysicsWorld>();
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps) {
        var job = new CollisionJob {
            projectiles = GetComponentDataFromEntity<Projectile>(true),
            queuedActions = ProjectileImpactManager.queuedProjectileEvents.AsParallelWriter(),
        }.Schedule(stepPhysicsWorld.Simulation, ref buildPhysicsWorldSystem.PhysicsWorld, inputDeps);
        job.Complete();
        return job;
    }
}
