using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

// This system updates all entities in the scene with a Translation component
public class VerticalPositionSystem : JobComponentSystem {
    EntityQuery m_Group;

    protected override void OnCreate() {
        // Cached access to a set of ComponentData based on a specific query
        m_Group = GetEntityQuery(typeof(Translation), ComponentType.ReadOnly<FieldCubeTag>());
    }

    [BurstCompile]
    struct VerticalPositionJob : IJobChunk {
        public float deltaTime;
        public ArchetypeChunkComponentType<Translation> translationType;

        public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex) {
            var chunkTranslation = chunk.GetNativeArray(translationType);
            for (var i = 0; i < chunk.Count; i++) {
                var translation = chunkTranslation[i].Value;
                chunkTranslation[i] = new Translation {
                    Value = new float3(translation.x, Perlin.getPerlin(deltaTime, translation.x, translation.z), translation.z)
                };
            }
        }
    }

    // OnUpdate runs on the main thread.
    protected override JobHandle OnUpdate(JobHandle inputDependencies) {
        // Explicitly declare:
        // - Read-Write access to Translation
        return new VerticalPositionJob() {
            deltaTime = Time.time,
            translationType = GetArchetypeChunkComponentType<Translation>()
        }.Schedule(m_Group, inputDependencies);
    }
}
