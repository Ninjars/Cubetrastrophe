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
struct RotateTurretJob : IJobForEach<LocalToWorld, PhysicsVelocity, Rotation, GunData, HasTarget, GunState> {
    private readonly static float PITCH_UPDATE_ANGLE = math.PI / 4f;
    private readonly static float HALF_PI = math.PI / 2f;
    private readonly static float DOUBLE_PI = math.PI * 2f;
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
            [ReadOnly] ref PhysicsVelocity velocity,
            ref Rotation rotation,
            [ReadOnly] ref GunData gun,
            [ReadOnly] ref HasTarget targetData,
            ref GunState state) {
        var targetVector = targetData.targetPosition - transform.Position;
        var approxTravelTime = math.sqrt(targetVector.x * targetVector.x + targetVector.y * targetVector.y + targetVector.z * targetVector.z) / gun.projectileVelocity;

        targetVector = (targetData.targetPosition + approxTravelTime * targetData.targetVelocity) - (transform.Position + approxTravelTime * velocity.Linear);

        // rotate relative vector into local space
        var globalRotation = gun.getParentRotation();
        targetVector = math.mul(math.mul(math.inverse(gun.neutralRotation), math.inverse(globalRotation)), targetVector);

        // rotation
        var targetRotation = math.atan2(targetVector.x, targetVector.z);
        var rotThisFrame = deltaTime * gun.rotationSpeed;
        var deltaRotation = targetRotation - state.currentRotation;
        if (deltaRotation > math.PI) {
            deltaRotation -= 2 * math.PI;
        } else if (deltaRotation < -math.PI) {
            deltaRotation += 2 * math.PI;
        }
        state.currentRotation += math.clamp(deltaRotation, -rotThisFrame, rotThisFrame);

        // pitch
        var targetPitch = (HALF_PI - calculateTargetPitch(targetVector, gun.projectileVelocity, 9.8f)) % math.PI;
        var pitchThisFrame = deltaTime * gun.pitchSpeed;
        
        var deltaPitch = (targetPitch - state.currentPitch);
        var deltaPitchClamped = math.clamp(deltaPitch, -pitchThisFrame, pitchThisFrame);
        state.currentPitch = math.clamp(state.currentPitch + deltaPitchClamped, HALF_PI - gun.maximumPitchDelta, HALF_PI + gun.maximumPitchDelta);

        // have to adjust current pitch by 90 degrees because polar coords have 0 as being straight upwards rather than forwards, which is what unity expects
        var localRotation = quaternion.EulerXYZ(state.currentPitch - HALF_PI, state.currentRotation, 0);
        rotation.Value = localRotation;


        // used for deciding whether to shoot
        state.targetRotationDelta = deltaRotation;
        state.targetPitchDelta = deltaPitch;
    }

    private float calculateTargetPitch(float3 relativeTargetVector, float projectileVelocity, float verticalAcceleration) {
        float dx = math.sqrt(relativeTargetVector.x * relativeTargetVector.x + relativeTargetVector.z * relativeTargetVector.z);
        float dy = relativeTargetVector.y;
        var v2 = projectileVelocity * projectileVelocity;
        var v4 = v2 * v2;
        var dx2 = dx * dx;
        return math.atan2(v2 - math.sqrt(v4 - verticalAcceleration * (verticalAcceleration * dx2 + 2 * dy * v2)), verticalAcceleration * dx);
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

        var tolerance = math.radians(3);
        if (math.abs(state.targetRotationDelta) > gun.shotDeviation + tolerance) { return; }
        if (math.abs(state.targetPitchDelta) > gun.shotDeviation + tolerance) { return; }

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
        var globalRotation = gun.getParentRotation();
        var localRotation = math.mul(globalRotation, math.mul(gun.neutralRotation, rotation.Value));

        var rnd = randomSources[threadIndex];
        var xOffset = quaternion.AxisAngle(math.up(), (rnd.NextFloat() * 2 - 1) * gun.shotDeviation);
        var yOffset = quaternion.AxisAngle(new float3(1, 0, 0), (rnd.NextFloat() * 2 - 1) * gun.shotDeviation);
        var bulletFacing = math.mul(math.mul(localRotation, xOffset), yOffset);

        var instance = commandBuffer.Instantiate(index, gun.projectileEntity);
        var position = transform.Position + math.rotate(localRotation.value, gun.projectileOffset);
        commandBuffer.SetComponent(index, instance, new Translation { Value = position });
        commandBuffer.SetComponent(index, instance, new Rotation { Value = bulletFacing });
        commandBuffer.SetComponent(index, instance, new PhysicsVelocity() {
            Linear = math.mul(bulletFacing, new float3(0, 0, gun.projectileVelocity)),
            Angular = float3.zero
        });
        commandBuffer.AddComponent(index, instance, new Projectile { firingEntity = entity });
        
        var muzzleFlash = commandBuffer.CreateEntity(index, muzzleFlashEntity);
        commandBuffer.SetComponent(index, muzzleFlash, new MuzzleFlashSystem.MuzzleFlashComponent {
            effectId = gun.muzzleFlashEffect,
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
            ComponentType.ReadOnly<PhysicsVelocity>(),
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
        randomSources = new NativeArray<Unity.Mathematics.Random>(System.Environment.ProcessorCount + 2, Allocator.TempJob);
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
            TurretInstantiator.onShotFired(muzzleFlash.effectId, muzzleFlash.position, muzzleFlash.rotation);
            PostUpdateCommands.DestroyEntity(entity);
        });
    }

    public struct MuzzleFlashComponent : IComponentData {
        public int effectId;
        public float3 position;
        public quaternion rotation;
    }
}