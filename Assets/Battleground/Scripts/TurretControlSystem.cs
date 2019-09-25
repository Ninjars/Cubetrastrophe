using System.Threading;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class TurretControlSystem : JobComponentSystem {
    EntityQuery queryGroup;
    EndSimulationEntityCommandBufferSystem bufferSystem;

    protected override void OnCreate() {
        // Cached access to a set of ComponentData based on a specific query
        queryGroup = GetEntityQuery(ComponentType.ReadOnly<Translation>(), ComponentType.ReadOnly<GunData>(), typeof(GunState));
        bufferSystem = World.Active.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }

    struct UpdateTurretJob : IJobForEach<Translation, GunData, GunState> {
        public EntityCommandBuffer commandBuffer;
        public float deltaTime;

        public void Execute([ReadOnly] ref Translation translation, [ReadOnly] ref GunData gun, ref GunState state) {
            if (state.shotsRemaining > 0) {
                if (state.currentFireInterval > 0) {
                    state.currentFireInterval = state.currentFireInterval - deltaTime;
                } else {
                    fireProjectile(ref translation, ref gun, ref state);
                }
            } else {
                if (state.currentReloadInterval > 0) {
                    state.currentReloadInterval = state.currentReloadInterval - deltaTime;
                } else {
                    state.shotsRemaining = gun.shotsPerReload;
                    state.currentReloadInterval = 0;
                    state.currentFireInterval = 0;
                }
            }
        }

        private void fireProjectile(ref Translation translation, ref GunData gun, ref GunState state) {
            state.shotsRemaining = state.shotsRemaining - 1;

            var instance = commandBuffer.Instantiate(gun.projectileEntity);
            commandBuffer.SetComponent(instance, new Translation { Value = translation.Value + gun.projectileOffset });

            if (state.shotsRemaining <= 0) {
                state.currentFireInterval = 0;
                state.currentReloadInterval = gun.reloadInterval;
                state.shotsRemaining = 0;

            } else {
                state.currentFireInterval = gun.fireInterval;
            }
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDependencies) {
        var job = new UpdateTurretJob() {
            commandBuffer = bufferSystem.CreateCommandBuffer(),
            deltaTime = Time.deltaTime
        }.ScheduleSingle(queryGroup, inputDependencies);
        bufferSystem.AddJobHandleForProducer(job);
        return job;
    }
}