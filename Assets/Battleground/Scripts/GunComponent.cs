using System;
using Unity.Entities;
using Unity.Mathematics;

[Serializable]
public struct GunData : IComponentData {
    public Entity projectileEntity;
    public float3 projectileOffset;
    public float projectileVelocity;
    public float reloadInterval;
    public float fireInterval;
    public int shotsPerReload;
    public float shotDeviationRadians;
    public float maximumPitchDelta;
    public float rotationSpeed;
    public float pitchSpeed;
}

public struct GunState : IComponentData {
    public float currentFireInterval;
    public float currentReloadInterval;
    public int shotsRemaining;
    public float currentRotation;
    public float currentPitch;
}

public struct Projectile : IComponentData {}
