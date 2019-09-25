using System;
using Unity.Entities;
using Unity.Mathematics;

[Serializable]
public struct GunData : IComponentData {
    public Entity projectileEntity;
    public float3 projectileOffset;
    public float reloadInterval;
    public float fireInterval;
    public int shotsPerReload;
}

public struct GunState : IComponentData {
    public float currentFireInterval;
    public float currentReloadInterval;
    public int shotsRemaining;
}