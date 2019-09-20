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
        [ReadOnly] public ArchetypeChunkComponentType<FieldCubeTag> tagType;

        public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex) {
            var chunkTranslation = chunk.GetNativeArray(translationType);
            for (var i = 0; i < chunk.Count; i++) {
                var translation = chunkTranslation[i].Value;
                chunkTranslation[i] = new Translation {
                    Value = new float3(translation.x, getPerlin(deltaTime, translation.x, translation.z), translation.z)
                };
            }
        }

        private float getPerlin(float offset, float x, float y) {
            var scale = 0.3f;
            return Mathf.Clamp01(Mathf.PerlinNoise(x * scale + 1000 + offset, y * scale + 2000));
        }
    }

    // OnUpdate runs on the main thread.
    protected override JobHandle OnUpdate(JobHandle inputDependencies) {
        // Explicitly declare:
        // - Read-Write access to Translation
        var type = GetArchetypeChunkComponentType<Translation>();
        var tah = GetArchetypeChunkComponentType<FieldCubeTag>(true);

        var job = new VerticalPositionJob() {
            deltaTime = Time.time,
            translationType = type,
            tagType = tah
        };
        return job.Schedule(m_Group, inputDependencies);
    }
}
