using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;
using Unity.Jobs;
using UnityEngine;
using ProceduralWorld.Simulation.Core;
using ProceduralWorld.Simulation.Components;
using ProceduralWorld.Simulation.Utils;
using CoreSimulationGroup = ProceduralWorld.Simulation.Core.SimulationSystemGroup;
using MathRandom = Unity.Mathematics.Random;

namespace ProceduralWorld.Simulation.Systems
{
    [UpdateInGroup(typeof(CoreSimulationGroup))]
    public partial struct DebugVisualizationSystem : ISystem
    {
        private EntityQuery _terrainQuery;
        private EntityQuery _configQuery;
        private EntityQuery _civilizationQuery;
        private EntityQuery _resourceQuery;
        private bool _isInitialized;
        private float _nextVisualizationUpdate;
        private const float VISUALIZATION_UPDATE_INTERVAL = 0.1f;
        private MathRandom _random;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _terrainQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<WorldTerrainData>(),
                ComponentType.ReadOnly<Core.SimulationConfig>()
            );
            _configQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<Core.SimulationConfig>()
            );
            _civilizationQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<CivilizationData>(),
                ComponentType.ReadOnly<Core.SimulationConfig>()
            );
            _resourceQuery = state.GetEntityQuery(typeof(ResourceData));
            _isInitialized = false;
            _nextVisualizationUpdate = 0;
            _random = MathRandom.CreateFromIndex(1234);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!_isInitialized)
            {
                if (!SystemAPI.HasSingleton<Core.SimulationConfig>())
                    return;

                var config = SystemAPI.GetSingleton<Core.SimulationConfig>();
                if (!config.EnableDebugVisualization)
                    return;

                _isInitialized = true;
            }

            var currentTime = (float)SystemAPI.Time.ElapsedTime;
            if (currentTime < _nextVisualizationUpdate)
                return;

            _nextVisualizationUpdate = currentTime + VISUALIZATION_UPDATE_INTERVAL;

            var job = new UpdateDebugVisualizationJob
            {
                DeltaTime = SystemAPI.Time.DeltaTime,
                Config = SystemAPI.GetSingleton<Core.SimulationConfig>(),
                Random = _random
            };

            state.Dependency = job.ScheduleParallel(state.Dependency);
            _random = job.Random;
        }

        [BurstCompile]
        private partial struct UpdateDebugVisualizationJob : IJobEntity
        {
            public float DeltaTime;
            [ReadOnly] public Core.SimulationConfig Config;
            public MathRandom Random;

            public void Execute(ref DebugVisualizationData visualization)
            {
                // Update visualization data
                visualization.IsVisible = true;
                visualization.Color = new float4(
                    Random.NextFloat(),
                    Random.NextFloat(),
                    Random.NextFloat(),
                    1.0f
                );
            }
        }
    }

    public struct DebugVisualizationData : IComponentData
    {
        public float3 Position;
        public float3 Scale;
        public float4 Color;
        public DebugVisualizationType Type;
        public bool IsVisible;
    }

    public enum DebugVisualizationType
    {
        Civilization,
        Structure,
        Resource,
        Terrain,
        System
    }
} 