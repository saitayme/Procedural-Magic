using Unity.Entities;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using ProceduralWorld.Simulation.Components;
using ProceduralWorld.Simulation.Core;
using UnitySimGroup = Unity.Entities.SimulationSystemGroup;
using UnityEngine;
using Unity.Burst.Intrinsics;

namespace ProceduralWorld.Simulation.Systems
{
    [UpdateInGroup(typeof(ProceduralWorld.Simulation.Core.SimulationSystemGroup))]
    public partial class SimulationDebugSystem : SystemBase
    {
        private NativeArray<CivilizationData> civilizations;
        private NativeArray<HistoricalEventData> recentEvents;
        private SimulationDebugData debugData;
        private EntityQuery civQuery;
        private EntityQuery eventQuery;
        private bool _isInitialized = false;
        private float _nextUpdate = 0f;
        private const float UPDATE_INTERVAL = 1f;
        private EntityQuery _configQuery;

        protected override void OnCreate()
        {
            civilizations = new NativeArray<CivilizationData>(0, Allocator.Persistent);
            recentEvents = new NativeArray<HistoricalEventData>(0, Allocator.Persistent);
            debugData = new SimulationDebugData
            {
                Civilizations = civilizations,
                RecentEvents = recentEvents
            };
            civQuery = GetEntityQuery(ComponentType.ReadOnly<CivilizationData>());
            eventQuery = GetEntityQuery(ComponentType.ReadOnly<HistoricalEventData>());
            _configQuery = GetEntityQuery(
                ComponentType.ReadOnly<SimulationConfig>()
            );
            _isInitialized = false;
            _nextUpdate = 0;
        }

        protected override void OnDestroy()
        {
            if (civilizations.IsCreated)
                civilizations.Dispose();
            if (recentEvents.IsCreated)
                recentEvents.Dispose();
        }

        protected override void OnUpdate()
        {
            if (!_isInitialized)
            {
                if (!SystemAPI.HasSingleton<SimulationConfig>())
                    return;

                var config = SystemAPI.GetSingleton<SimulationConfig>();
                if (!config.EnableDebug)
                    return;

                _isInitialized = true;
            }

            var currentTime = (float)SystemAPI.Time.ElapsedTime;
            if (currentTime < _nextUpdate)
                return;

            _nextUpdate = currentTime + UPDATE_INTERVAL;

            var simulationConfig = SystemAPI.GetSingleton<SimulationConfig>();
            Debug.Log($"Simulation Time: {currentTime:F2}, Delta Time: {SystemAPI.Time.DeltaTime:F3}, Speed: {simulationConfig.TimeScale:F2}x");

            if (SystemAPI.HasSingleton<SimulationConfig>())
            {
                var configSingleton = SystemAPI.GetSingleton<SimulationConfig>();
                debugData.CurrentYear = (int)(currentTime / 365.25f); // Convert time to years
                debugData.TimeScale = configSingleton.TimeScale;
                debugData.DeltaTime = SystemAPI.Time.DeltaTime;

                // Update civilizations array
                int civCount = civQuery.CalculateEntityCount();
                if (civCount != civilizations.Length)
                {
                    if (civilizations.IsCreated)
                        civilizations.Dispose();
                    civilizations = new NativeArray<CivilizationData>(civCount, Allocator.Persistent);
                    debugData.Civilizations = civilizations;
                }
                if (civCount > 0)
                {
                    var civArray = civQuery.ToComponentDataArray<CivilizationData>(Allocator.Temp);
                    civArray.CopyTo(civilizations);
                    civArray.Dispose();
                }

                // Update recent events
                int eventCount = math.min(eventQuery.CalculateEntityCount(), 10); // Keep last 10 events
                if (eventCount != recentEvents.Length)
                {
                    if (recentEvents.IsCreated)
                        recentEvents.Dispose();
                    recentEvents = new NativeArray<HistoricalEventData>(eventCount, Allocator.Persistent);
                    debugData.RecentEvents = recentEvents;
                }
                if (eventCount > 0)
                {
                    var eventArray = eventQuery.ToComponentDataArray<HistoricalEventData>(Allocator.Temp);
                    for (int i = 0; i < eventCount; i++)
                        recentEvents[i] = eventArray[eventArray.Length - eventCount + i];
                    eventArray.Dispose();
                }
            }
        }

        public SimulationDebugData GetDebugData()
        {
            return debugData;
        }
    }
} 