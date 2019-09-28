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
struct UpdateTurretJob : IJobForEach<LocalToWorld, Rotation, GunData, HasTarget, GunState> {
    public EntityCommandBuffer commandBuffer;
    public float deltaTime;
    [DeallocateOnJobCompletion]
    [NativeDisableParallelForRestriction]
    public NativeArray<Unity.Mathematics.Random> randomSources;
    [NativeSetThreadIndex]
    private int threadIndex;

    private float getRotationDelta(float targetAngle, float currentAngle, float maxAngle) {
        float deltaAngle = targetAngle - currentAngle;
        if (deltaAngle > math.PI) deltaAngle -= 2 * math.PI;
        if (deltaAngle < -math.PI) deltaAngle += 2 * math.PI;
        if (math.abs(deltaAngle) > maxAngle) {
            if (deltaAngle < 0) {
                return -maxAngle;
            } else {
                return maxAngle;
            }
        } else {
            return deltaAngle;
        }
    }

    public void Execute(
            [ReadOnly] ref LocalToWorld transform,
            ref Rotation rotation,
            [ReadOnly] ref GunData gun,
            [ReadOnly] ref HasTarget targetData,
            ref GunState state) {

        var targetFacingQuaternion = quaternion.LookRotationSafe(targetData.targetPosition - transform.Position, math.up());
        var targetAxisRotations = axisAngles(targetFacingQuaternion);
        var deltaX = getRotationDelta(targetAxisRotations.x, state.currentPitch, gun.pitchSpeed * deltaTime);
        var deltaY = getRotationDelta(targetAxisRotations.y, state.currentRotation, gun.rotationSpeed * deltaTime);

        state.currentPitch = math.min(gun.maximumPitchDelta, math.max(-gun.maximumPitchDelta, state.currentPitch + deltaX));
        state.currentRotation = state.currentRotation + deltaY ;
        rotation.Value = quaternion.EulerXYZ(state.currentPitch, state.currentRotation, 0);

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
    
    private float3 axisAngles(quaternion quaternion) {
        float4 rotation = math.normalizesafe(quaternion).value;
        var angle = 2 * math.acos(rotation.w);
        var constant = math.sqrt(1 - rotation.w * rotation.w);
        float3 axisOfRotation;
        if (constant < 0.001) {
            // angle is effectively 0
            axisOfRotation = new float3(1, 0, 0);
        } else {
            axisOfRotation = new float3(rotation.x / constant, rotation.y / constant, rotation.z / constant);
        }
        return axisOfRotation * angle;
    }
    private void fireProjectile(ref LocalToWorld transform, ref Rotation rotation, ref GunData gun, ref GunState state) {
        state.shotsRemaining = state.shotsRemaining - 1;

        var rnd = randomSources[threadIndex];
        var xOffset = quaternion.AxisAngle(math.up(), (rnd.NextFloat() * 2 - 1) * gun.shotDeviation);
        var yOffset = quaternion.AxisAngle(new float3(1, 0, 0), (rnd.NextFloat() * 2 - 1) * gun.shotDeviation);
        var bulletFacing = math.mul(math.mul(rotation.Value, xOffset), yOffset);

        var instance = commandBuffer.Instantiate(gun.projectileEntity);
        commandBuffer.SetComponent(instance, new Translation { Value = transform.Position + math.rotate(rotation.Value.value, gun.projectileOffset) });
        commandBuffer.SetComponent(instance, new Rotation { Value = bulletFacing });
        commandBuffer.SetComponent(instance, new PhysicsVelocity() {
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
    private EntityQuery queryGroup;
    private EndSimulationEntityCommandBufferSystem bufferSystem;
    private NativeArray<Unity.Mathematics.Random> randomSources;
    private Unity.Mathematics.Random rnd;

    protected override void OnCreate() {
        // Cached access to a set of ComponentData based on a specific query
        queryGroup = GetEntityQuery(
            ComponentType.ReadOnly<LocalToWorld>(),
            typeof(Rotation),
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
        var job = new UpdateTurretJob() {
            commandBuffer = bufferSystem.CreateCommandBuffer(),
            deltaTime = Time.deltaTime,
            randomSources = randomSources,
        }.ScheduleSingle(queryGroup, inputDependencies);
        bufferSystem.AddJobHandleForProducer(job);
        return job;
    }
}