using System;
using Unity.Entities;
using Unity.Mathematics;
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
    public float3 localRotationAxis;
    internal float3 localPitchAxis;
}

public struct GunState : IComponentData {
    public float currentFireInterval;
    public float currentReloadInterval;
    public int shotsRemaining;
    public float currentRotation;
    public float currentPitch;
    public float targetAngle;
}

public struct Projectile : IComponentData {
    public Entity firingEntity;
}
