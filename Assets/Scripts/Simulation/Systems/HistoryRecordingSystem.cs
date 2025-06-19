using Unity.Entities;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using ProceduralWorld.Simulation.Components;
using ProceduralWorld.Simulation.Core;
using UnitySimGroup = Unity.Entities.SimulationSystemGroup;
using UnityEngine;
using Unity.Burst.Intrinsics;
using Unity.Jobs;
using CoreSimulationGroup = ProceduralWorld.Simulation.Core.SimulationSystemGroup;
using ProceduralWorld.Simulation.Utils;
using MathRandom = Unity.Mathematics.Random;

namespace ProceduralWorld.Simulation.Systems
{
    [BurstCompile]
    [UpdateInGroup(typeof(CoreSimulationGroup))]
    public partial struct HistoryRecordingSystem : ISystem
    {
        private EntityQuery _historyQuery;
        private EntityQuery _configQuery;
        private bool _isInitialized;
        private float _nextUpdate;
        private const float UPDATE_INTERVAL = 1.0f;
        private ComponentTypeHandle<WorldHistoryData> _historyHandle;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            var builder = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<WorldHistoryData, Core.SimulationConfig>();
            _historyQuery = state.GetEntityQuery(builder);
            
            builder = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<Core.SimulationConfig>();
            _configQuery = state.GetEntityQuery(builder);

            _isInitialized = false;
            _nextUpdate = 0;
            _historyHandle = state.GetComponentTypeHandle<WorldHistoryData>(false);

            Debug.Log("[HistoryRecordingSystem] System created");
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!_isInitialized)
            {
                if (!SystemAPI.HasSingleton<Core.SimulationConfig>())
                    return;

                var config = SystemAPI.GetSingleton<Core.SimulationConfig>();
                if (!config.EnableHistoryRecording)
                    return;

                _isInitialized = true;
            }

            var currentTime = (float)SystemAPI.Time.ElapsedTime;
            if (currentTime < _nextUpdate)
                return;

            _nextUpdate = currentTime + UPDATE_INTERVAL;

            _historyHandle.Update(ref state);

            var job = new UpdateHistoryJob
            {
                DeltaTime = SystemAPI.Time.DeltaTime,
                Config = SystemAPI.GetSingleton<Core.SimulationConfig>(),
                HistoryHandle = _historyHandle
            };

            state.Dependency = job.ScheduleParallel(_historyQuery, state.Dependency);
        }

        [BurstCompile]
        private struct UpdateHistoryJob : IJobChunk
        {
            public float DeltaTime;
            [ReadOnly] public Core.SimulationConfig Config;
            public ComponentTypeHandle<WorldHistoryData> HistoryHandle;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var histories = chunk.GetNativeArray(ref HistoryHandle);

                for (int i = 0; i < chunk.Count; i++)
                {
                    var history = histories[i];

                    // Update history values using temporary variables
                    float newTimelineProgress = history.TimelineProgress + Config.TimelineProgressRate * DeltaTime;

                    // Apply limits
                    history.TimelineProgress = math.min(newTimelineProgress, Config.MaxTimelineProgress);

                    histories[i] = history;
                }
            }
        }
    }
} 