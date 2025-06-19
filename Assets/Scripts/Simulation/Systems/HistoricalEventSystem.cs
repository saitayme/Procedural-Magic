using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;
using Unity.Jobs;
using Unity.Burst.Intrinsics;
using ProceduralWorld.Simulation.Core;
using ProceduralWorld.Simulation.Components;
using ProceduralWorld.Simulation.Utils;
using UnityEngine;
using CoreSimulationGroup = ProceduralWorld.Simulation.Core.SimulationSystemGroup;
using MathRandom = Unity.Mathematics.Random;

namespace ProceduralWorld.Simulation.Systems
{
    [BurstCompile]
    [UpdateInGroup(typeof(CoreSimulationGroup))]
    public partial struct HistoricalEventSystem : ISystem
    {
        private EntityQuery _eventQuery;
        private EntityQuery _configQuery;
        private bool _isInitialized;
        private float _nextUpdate;
        private const float UPDATE_INTERVAL = 1.0f;
        private ComponentTypeHandle<HistoricalEventData> _eventHandle;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _eventQuery = state.GetEntityQuery(
                ComponentType.ReadWrite<HistoricalEventData>(),
                ComponentType.ReadOnly<Core.SimulationConfig>()
            );
            
            _configQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<Core.SimulationConfig>()
            );

            _isInitialized = false;
            _nextUpdate = 0;
            _eventHandle = state.GetComponentTypeHandle<HistoricalEventData>(false);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!_isInitialized)
            {
                if (!SystemAPI.HasSingleton<Core.SimulationConfig>())
                    return;

                var config = SystemAPI.GetSingleton<Core.SimulationConfig>();
                if (!config.EnableHistoricalEvents)
                    return;

                _isInitialized = true;
            }

            var currentTime = (float)SystemAPI.Time.ElapsedTime;
            if (currentTime < _nextUpdate)
                return;

            _nextUpdate = currentTime + UPDATE_INTERVAL;

            _eventHandle.Update(ref state);

            var job = new UpdateEventJob
            {
                DeltaTime = SystemAPI.Time.DeltaTime,
                Config = SystemAPI.GetSingleton<Core.SimulationConfig>(),
                EventHandle = _eventHandle
            };

            state.Dependency = job.ScheduleParallel(_eventQuery, state.Dependency);
        }

        [BurstCompile]
        private struct UpdateEventJob : IJobChunk
        {
            public float DeltaTime;
            [ReadOnly] public Core.SimulationConfig Config;
            public ComponentTypeHandle<HistoricalEventData> EventHandle;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var events = chunk.GetNativeArray(ref EventHandle);

                for (int i = 0; i < chunk.Count; i++)
                {
                    var evt = events[i];

                    // Update event values
                    evt.Significance = math.min(evt.Significance + Config.EventSignificanceRate * DeltaTime, Config.MaxEventSignificance);
                    evt.Influence = math.min(evt.Influence + Config.EventInfluenceRate * DeltaTime, Config.MaxEventInfluence);

                    events[i] = evt;
                }
            }
        }
    }
} 