using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[Serializable]
public struct GunData : IComponentData {
    public Entity projectileEntity;
    public float3 projectileOffset;
    public float projectileVelocity;
    public float reloadInterval;
    public float fireInterval;
    public int shotsPerReload;
    public float shotDeviation;
    public float maximumPitchDelta;
    public float rotationSpeed;
    public float pitchSpeed;
    public quaternion neutralRotation;
    public Rotation? parentRotation;

    public quaternion getParentRotation() {
        if (parentRotation.HasValue) {
            return parentRotation.Value.Value;
        } else {
            return quaternion.identity;
        }
    }
}

public struct GunState : IComponentData {
    public float currentFireInterval;
    public float currentReloadInterval;
    public int shotsRemaining;
    public float currentRotation;
    public float currentPitch;
    public float targetRotationDelta;
    public float targetPitchDelta;
}

public struct Projectile : IComponentData {
    public Entity firingEntity;
}
