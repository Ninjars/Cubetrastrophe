using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Physics;
using Unity.Physics.Systems;
using UnityEngine;

[UpdateAfter(typeof(StepPhysicsWorld))]
[UpdateBefore(typeof(EndFramePhysicsSystem))]
public class CollisionSystem : JobComponentSystem {

    struct CollisionJob : ICollisionEventsJob {
        public EntityCommandBuffer commandBuffer;

        public void Execute(CollisionEvent ev) {
            Entity a = ev.Entities.EntityA;
            Entity b = ev.Entities.EntityB;
            Debug.Log($"collision event: Normal {ev.Normal} Entities: {a}, {b}");
            commandBuffer.DestroyEntity(a);
        }
    }

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
            commandBuffer = bufferSystem.CreateCommandBuffer()
        }.Schedule(stepPhysicsWorld.Simulation, ref buildPhysicsWorldSystem.PhysicsWorld, inputDeps);

        bufferSystem.AddJobHandleForProducer(inputDeps);
        job.Complete();
        return job;
    }
}
