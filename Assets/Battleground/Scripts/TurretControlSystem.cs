using System.Threading;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

struct UpdateTurretJob : IJobForEach<LocalToWorld, Rotation, GunData, GunState> {
    public EntityCommandBuffer commandBuffer;
    public float deltaTime;

    public void Execute(
            [ReadOnly] ref LocalToWorld transform,
            ref Rotation rotation,
            [ReadOnly] ref GunData gun,
            ref GunState state) {
        rotation.Value = math.mul(math.normalizesafe(rotation.Value), quaternion.AxisAngle(math.up(), 0.01f));
        if (state.shotsRemaining > 0) {
            if (state.currentFireInterval > 0) {
                state.currentFireInterval = state.currentFireInterval - deltaTime;
            } else {
                fireProjectile(ref transform, ref rotation, ref gun, ref state);
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

    private void fireProjectile(ref LocalToWorld transform, ref Rotation rotation, ref GunData gun, ref GunState state) {
        state.shotsRemaining = state.shotsRemaining - 1;

        var instance = commandBuffer.Instantiate(gun.projectileEntity);
        commandBuffer.SetComponent(instance, new Translation { Value = transform.Position + math.rotate(rotation.Value.value, gun.projectileOffset) });
        commandBuffer.SetComponent(instance, new Rotation { Value = rotation.Value });
        commandBuffer.SetComponent(instance, new PhysicsVelocity() {
            Linear = math.mul(rotation.Value.value, new float3(gun.projectileVelocity, 0, 0)),
            Angular = float3.zero
        });

        if (state.shotsRemaining <= 0) {
            state.currentFireInterval = 0;
            state.currentReloadInterval = gun.reloadInterval;
            state.shotsRemaining = 0;

        } else {
            state.currentFireInterval = gun.fireInterval;
        }
    }
}

public class TurretControlSystem : JobComponentSystem {
    EntityQuery queryGroup;
    EndSimulationEntityCommandBufferSystem bufferSystem;

    protected override void OnCreate() {
        // Cached access to a set of ComponentData based on a specific query
        queryGroup = GetEntityQuery(
            ComponentType.ReadOnly<LocalToWorld>(),
            typeof(Rotation),
            ComponentType.ReadOnly<GunData>(),
            typeof(GunState));
        bufferSystem = World.Active.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
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