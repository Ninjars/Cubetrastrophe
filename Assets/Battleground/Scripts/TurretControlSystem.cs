using System.Threading;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

// credit for aid with the quaternion -> axis angle maths https://www.euclideanspace.com/maths/geometry/rotations/conversions/quaternionToAngle/index.htm
[BurstCompile]
struct RotateTurretJob : IJobForEach<LocalToWorld, Rotation, GunData, HasTarget, GunState> {
    public float deltaTime;

    private float clipRotation(float value, float maxValue) {
        if (math.abs(value) > maxValue) {
            if (value < 0) {
                return -maxValue;
            } else {
                return maxValue;
            }
        } else {
            return value;
        }
    }

    public void Execute(
            [ReadOnly] ref LocalToWorld transform,
            ref Rotation rotation,
            [ReadOnly] ref GunData gun,
            [ReadOnly] ref HasTarget targetData,
            ref GunState state) {
        var targetFacingQuaternion = quaternion.LookRotationSafe(targetData.targetPosition - transform.Position, gun.localRotationAxis);
        var deltaRotation = math.mul(math.inverse(rotation.Value), targetFacingQuaternion);
        var targetAxisRotations = MathUtils.axisAngles(deltaRotation);
        state.targetAngle = targetAxisRotations.y;

        var deltaX = clipRotation(targetAxisRotations.x, gun.pitchSpeed * deltaTime);
        var deltaY = clipRotation(targetAxisRotations.y, gun.rotationSpeed * deltaTime);

        var rotationAxis = MathUtils.axisAngles(rotation.Value);
        state.currentPitch = math.min(gun.maximumPitchDelta, math.max(-gun.maximumPitchDelta, state.currentPitch + deltaX));
        state.currentRotation = (state.currentRotation + deltaY) % (math.PI * 2);

        var localRotation = quaternion.EulerXYZ(state.currentPitch, state.currentRotation, 0);
        var worldRotation = math.mul(gun.neutralRotation, localRotation);
        rotation.Value = worldRotation;
    }
}

struct FireTurretJob : IJobForEachWithEntity<LocalToWorld, Rotation, GunData, GunState, HasTarget> {
    public EntityCommandBuffer.Concurrent commandBuffer;
    public float deltaTime;
    [DeallocateOnJobCompletion]
    [NativeDisableParallelForRestriction]
    public NativeArray<Unity.Mathematics.Random> randomSources;
    [NativeSetThreadIndex]
    private int threadIndex;

    public void Execute(
            Entity entity, int index,
            [ReadOnly] ref LocalToWorld transform,
            [ReadOnly] ref Rotation rotation,
            [ReadOnly] ref GunData gun,
            ref GunState state,
            [ReadOnly] ref HasTarget targetData) {

        if (math.abs(state.targetAngle) > gun.shotDeviation / 2f) { return; }

        if (state.shotsRemaining > 0) {
            if (state.currentFireInterval > 0) {
                state.currentFireInterval = state.currentFireInterval - deltaTime;
            } else {
                fireProjectile(index, ref transform, ref rotation, ref gun, ref state);
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

    private void fireProjectile(int index, ref LocalToWorld transform, ref Rotation rotation, ref GunData gun, ref GunState state) {
        state.shotsRemaining = state.shotsRemaining - 1;

        var rnd = randomSources[threadIndex];
        var xOffset = quaternion.AxisAngle(math.up(), (rnd.NextFloat() * 2 - 1) * gun.shotDeviation);
        var yOffset = quaternion.AxisAngle(new float3(1, 0, 0), (rnd.NextFloat() * 2 - 1) * gun.shotDeviation);
        var bulletFacing = math.mul(math.mul(rotation.Value, xOffset), yOffset);

        var instance = commandBuffer.Instantiate(index, gun.projectileEntity);
        commandBuffer.SetComponent(index, instance, new Translation { Value = transform.Position + math.rotate(rotation.Value.value, gun.projectileOffset) });
        commandBuffer.SetComponent(index, instance, new Rotation { Value = bulletFacing });
        commandBuffer.SetComponent(index, instance, new PhysicsVelocity() {
            Linear = math.mul(bulletFacing, new float3(0, 0, gun.projectileVelocity)),
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
    private EntityQuery rotationQueryGroup;
    private EntityQuery firingQueryGroup;
    private EndSimulationEntityCommandBufferSystem bufferSystem;
    private NativeArray<Unity.Mathematics.Random> randomSources;
    private Unity.Mathematics.Random rnd;

    protected override void OnCreate() {
        // Cached access to a set of ComponentData based on a specific query
        rotationQueryGroup = GetEntityQuery(
            ComponentType.ReadOnly<LocalToWorld>(),
            typeof(Rotation),
            ComponentType.ReadOnly<GunData>(),
            ComponentType.ReadOnly<HasTarget>(),
            typeof(GunState));
        firingQueryGroup = GetEntityQuery(
            ComponentType.ReadOnly<LocalToWorld>(),
            ComponentType.ReadOnly<Rotation>(),
            ComponentType.ReadOnly<GunData>(),
            ComponentType.ReadOnly<HasTarget>(),
            typeof(GunState));
        bufferSystem = World.Active.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        rnd = new Unity.Mathematics.Random((uint)UnityEngine.Random.Range(int.MinValue, int.MaxValue));
    }

    protected override JobHandle OnUpdate(JobHandle inputDependencies) {
        randomSources = new NativeArray<Unity.Mathematics.Random>(System.Environment.ProcessorCount + 1, Allocator.TempJob);
        for (int i = 0; i < randomSources.Length; i++) {
            randomSources[i] = new Unity.Mathematics.Random((uint) rnd.NextInt());
        }
        var rotateJob = new RotateTurretJob() {
            deltaTime = Time.deltaTime,
        }.Schedule(rotationQueryGroup, inputDependencies);

        var fireJob = new FireTurretJob() {
            commandBuffer = bufferSystem.CreateCommandBuffer().ToConcurrent(),
            deltaTime = Time.deltaTime,
            randomSources = randomSources,
        }.Schedule(firingQueryGroup, rotateJob);

        bufferSystem.AddJobHandleForProducer(fireJob);
        return fireJob;
    }
}