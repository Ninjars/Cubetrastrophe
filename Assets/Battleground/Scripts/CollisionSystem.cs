// using Unity.Collections;
// using Unity.Entities;
// using Unity.Jobs;
// using Unity.Physics;
// using Unity.Physics.Systems;
// using UnityEngine;

// [UpdateAfter(typeof(StepPhysicsWorld))]
// [UpdateBefore(typeof(EndFramePhysicsSystem))]
// public class CollisionSystem : JobComponentSystem {

//     struct CollisionJob : ICollisionEventsJob {
//         [ReadOnly] public PhysicsWorld physicsWorld;

//         public void Execute(CollisionEvent ev) {
//             Entity a = physicsWorld.Bodies[ev.BodyIndices.BodyAIndex].Entity;
//             Entity b = physicsWorld.Bodies[ev.BodyIndices.BodyBIndex].Entity;
//             Debug.Log($"collision event: {ev}. Entities: {a}, {b}");
//         }
//     }


//     BuildPhysicsWorld buildPhysicsWorldSystem;
//     StepPhysicsWorld stepPhysicsWorld;
//     EndFramePhysicsSystem endFramePhysicsSystem;

//     protected override void OnCreate() {
//         buildPhysicsWorldSystem = World.GetOrCreateSystem<BuildPhysicsWorld>();
//         stepPhysicsWorld = World.GetOrCreateSystem<StepPhysicsWorld>();
//     }

//     protected override JobHandle OnUpdate(JobHandle inputDeps) {
//         Debug.Log($"collision system running: {Time.time}"); // this runs correctly

//         inputDeps = JobHandle.CombineDependencies(inputDeps, buildPhysicsWorldSystem.FinalJobHandle);
//         inputDeps = JobHandle.CombineDependencies(inputDeps, stepPhysicsWorld.FinalJobHandle);

//         var physicsWorld = buildPhysicsWorldSystem.PhysicsWorld;

//         var collisionJob = new CollisionJob {
//             physicsWorld = physicsWorld
//         };
//         JobHandle collisionHandle = collisionJob.Schedule(stepPhysicsWorld.Simulation, ref physicsWorld, inputDeps);

//         return collisionHandle;

//     }

// }