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
// [BurstCompile]
struct RotateTurretJob : IJobForEach<LocalToWorld, Rotation, GunData, HasTarget, GunState> {
    private static float HALF_PI = math.PI / 2f;
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
        var targetVector = math.normalize(targetData.targetPosition - transform.Position);
        float3 localTargetVector = math.mul(rotation.Value, targetVector);
        float3 localForwardVector = math.mul(rotation.Value, transform.Forward);
        var angleAboutY = math.atan2(localTargetVector.x, localTargetVector.z) - math.atan2(localForwardVector.x, localForwardVector.z);
        if (angleAboutY > math.PI) {
            angleAboutY -= 2 * math.PI;
        } else if (angleAboutY < -math.PI) {
            angleAboutY += 2 * math.PI;
        }
        var yThisFrame = deltaTime * gun.rotationSpeed;
        state.currentRotation += math.clamp(angleAboutY, -yThisFrame, yThisFrame);

        var deltaForwardOffset = math.sqrt(localForwardVector.z * localForwardVector.z + localForwardVector.x * localForwardVector.x);
        var deltaTargetOffset = math.sqrt(localTargetVector.z * localTargetVector.z + localTargetVector.x * localTargetVector.x);
        var angleAboutX = math.atan2(localTargetVector.y, deltaTargetOffset) - math.atan2(localForwardVector.y, deltaForwardOffset);
        var xThisFrame = deltaTime * gun.pitchSpeed;
        angleAboutX = math.clamp(angleAboutX, -xThisFrame, xThisFrame);
        state.currentPitch = math.clamp(state.currentPitch - angleAboutX, -gun.maximumPitchDelta, gun.maximumPitchDelta);

        Debug.Log(""
        // + $" dy {localTargetVector.y - localForwardVector.y} dz {localTargetVector.z - localForwardVector.z}"
        + $" rotate {math.degrees(angleAboutY)}"
        + $" pitch {math.degrees(angleAboutX)}"
        );

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
    public EntityArchetype muzzleFlashEntity;
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
                fireProjectile(entity, index, ref transform, ref rotation, ref gun, ref state);
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

    private void fireProjectile(Entity entity, int index, ref LocalToWorld transform, ref Rotation rotation, ref GunData gun, ref GunState state) {
        state.shotsRemaining = state.shotsRemaining - 1;

        var rnd = randomSources[threadIndex];
        var xOffset = quaternion.AxisAngle(math.up(), (rnd.NextFloat() * 2 - 1) * gun.shotDeviation);
        var yOffset = quaternion.AxisAngle(new float3(1, 0, 0), (rnd.NextFloat() * 2 - 1) * gun.shotDeviation);
        var bulletFacing = math.mul(math.mul(rotation.Value, xOffset), yOffset);

        var instance = commandBuffer.Instantiate(index, gun.projectileEntity);
        var position = transform.Position + math.rotate(rotation.Value.value, gun.projectileOffset);
        commandBuffer.SetComponent(index, instance, new Translation { Value = position });
        commandBuffer.SetComponent(index, instance, new Rotation { Value = bulletFacing });
        commandBuffer.SetComponent(index, instance, new PhysicsVelocity() {
            Linear = math.mul(bulletFacing, new float3(0, 0, gun.projectileVelocity)),
            Angular = float3.zero
        });
        commandBuffer.AddComponent(index, instance, new Projectile { firingEntity = entity });

        var muzzleFlash = commandBuffer.CreateEntity(index, muzzleFlashEntity);
        commandBuffer.SetComponent(index, muzzleFlash, new MuzzleFlashSystem.MuzzleFlashComponent {
            position = position,
            rotation = bulletFacing,
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
    private EntityArchetype muzzleFlashEntity;

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
        muzzleFlashEntity = World.Active.EntityManager.CreateArchetype(
            typeof(MuzzleFlashSystem.MuzzleFlashComponent)
        );
    }

    protected override JobHandle OnUpdate(JobHandle inputDependencies) {
        randomSources = new NativeArray<Unity.Mathematics.Random>(System.Environment.ProcessorCount + 1, Allocator.TempJob);
        for (int i = 0; i < randomSources.Length; i++) {
            randomSources[i] = new Unity.Mathematics.Random((uint)rnd.NextInt());
        }
        var rotateJob = new RotateTurretJob() {
            deltaTime = Time.deltaTime,
        }.Schedule(rotationQueryGroup, inputDependencies);

        var fireJob = new FireTurretJob() {
            muzzleFlashEntity = muzzleFlashEntity,
            commandBuffer = bufferSystem.CreateCommandBuffer().ToConcurrent(),
            deltaTime = Time.deltaTime,
            randomSources = randomSources,
        }.Schedule(firingQueryGroup, rotateJob);

        bufferSystem.AddJobHandleForProducer(fireJob);
        return fireJob;
    }
}

public class MuzzleFlashSystem : ComponentSystem {
    private EntityQuery query;

    protected override void OnCreate() {
        query = GetEntityQuery(ComponentType.ReadOnly<MuzzleFlashComponent>());
    }

    protected override void OnUpdate() {
        Entities.ForEach((Entity entity, ref MuzzleFlashComponent muzzleFlash) => {
            Turret.onShotFired(muzzleFlash.position, muzzleFlash.rotation);
            PostUpdateCommands.DestroyEntity(entity);
        });
    }

    public struct MuzzleFlashComponent : IComponentData {
        public float3 position;
        public quaternion rotation;
    }
}