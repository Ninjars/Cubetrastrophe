using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Physics;
using Unity.Physics.Systems;
using UnityEngine;

struct CollisionJob : ICollisionEventsJob {
    public EntityCommandBuffer commandBuffer;
    [ReadOnly] internal ComponentDataFromEntity<Projectile> projectiles;

    public void Execute(CollisionEvent ev) {
        Entity a = ev.Entities.EntityA;
        Entity b = ev.Entities.EntityB;
        if (projectiles.Exists(a)) {
            commandBuffer.DestroyEntity(a);
        }
        if (projectiles.Exists(b)) {
            commandBuffer.DestroyEntity(b);
        }
    }
}

[UpdateAfter(typeof(StepPhysicsWorld))]
[UpdateBefore(typeof(EndFramePhysicsSystem))]
public class CollisionSystem : JobComponentSystem {

    BuildPhysicsWorld buildPhysicsWorldSystem;
    StepPhysicsWorld stepPhysicsWorld;
    EndSimulationEntityCommandBufferSystem bufferSystem;

    protected override void OnCreate() {
        buildPhysicsWorldSystem = World.GetOrCreateSystem<BuildPhysicsWorld>();
        stepPhysicsWorld = World.GetOrCreateSystem<StepPhysicsWorld>();
        bufferSystem = World.Active.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps) {
        var job = new CollisionJob {
            commandBuffer = bufferSystem.CreateCommandBuffer(),
            projectiles = GetComponentDataFromEntity<Projectile>(true),
        }.Schedule(stepPhysicsWorld.Simulation, ref buildPhysicsWorldSystem.PhysicsWorld, inputDeps);

        bufferSystem.AddJobHandleForProducer(inputDeps);
        job.Complete();
        return job;
    }
}
