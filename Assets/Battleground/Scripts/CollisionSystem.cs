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
        [ReadOnly] public PhysicsWorld physicsWorld;
        [ReadOnly] public NativeSlice<RigidBody> Bodies;

        public void Execute(CollisionEvent ev) {
            Entity a = physicsWorld.Bodies[ev.BodyIndices.BodyAIndex].Entity;
            Entity b = physicsWorld.Bodies[ev.BodyIndices.BodyBIndex].Entity;
            Debug.Log($"collision event: Normal {ev.Normal} Entities: {a}, {b}");
        }
    }

    BuildPhysicsWorld buildPhysicsWorldSystem;
    StepPhysicsWorld stepPhysicsWorld;
    EndFramePhysicsSystem endFramePhysicsSystem;

    protected override void OnCreate() {
        buildPhysicsWorldSystem = World.GetOrCreateSystem<BuildPhysicsWorld>();
        stepPhysicsWorld = World.GetOrCreateSystem<StepPhysicsWorld>();
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps) {
        SimulationCallbacks.Callback testCollisionEventCallback = (ref ISimulation simulation, ref PhysicsWorld world, JobHandle inDeps) => {
            return new CollisionJob {
                physicsWorld = world,
                Bodies = world.Bodies
            }.Schedule(simulation, ref world, inDeps);
        };

        stepPhysicsWorld.EnqueueCallback(SimulationCallbacks.Phase.PostSolveJacobians, testCollisionEventCallback, inputDeps);

        return inputDeps;
    }
}
