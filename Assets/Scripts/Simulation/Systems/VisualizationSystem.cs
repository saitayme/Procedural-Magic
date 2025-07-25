using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Jobs;
using UnityEngine;
using ProceduralWorld.Simulation.Core;
using ProceduralWorld.Simulation.Components;
using ProceduralWorld.Simulation.Utils;
using CoreSimulationGroup = ProceduralWorld.Simulation.Core.SimulationSystemGroup;
using TerrainData = ProceduralWorld.Simulation.Components.TerrainData;

namespace ProceduralWorld.Simulation.Systems
{
    [UpdateInGroup(typeof(CoreSimulationGroup))]
    public partial struct VisualizationSystem : ISystem
    {
        private EntityQuery _terrainQuery;
        private EntityQuery _configQuery;
        private bool _isInitialized;
        private float _nextUpdate;
        private const float UPDATE_INTERVAL = 0.1f;
        private ComponentTypeHandle<TerrainData> _terrainHandle;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _terrainQuery = state.GetEntityQuery(
                ComponentType.ReadWrite<TerrainData>(),
                ComponentType.ReadOnly<Core.SimulationConfig>()
            );
            
            _configQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<Core.SimulationConfig>()
            );

            _isInitialized = false;
            _nextUpdate = 0;
            _terrainHandle = state.GetComponentTypeHandle<TerrainData>(false);
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
                if (!config.EnableVisualizationSystem)
                    return;

                _isInitialized = true;
            }

            var currentTime = (float)SystemAPI.Time.ElapsedTime;
            if (currentTime < _nextUpdate)
                return;

            _nextUpdate = currentTime + UPDATE_INTERVAL;

            _terrainHandle.Update(ref state);

            var job = new UpdateVisualizationJob
            {
                DeltaTime = SystemAPI.Time.DeltaTime,
                Config = SystemAPI.GetSingleton<Core.SimulationConfig>(),
                TerrainHandle = _terrainHandle
            };

            state.Dependency = job.ScheduleParallel(_terrainQuery, state.Dependency);
        }

        [BurstCompile]
        private struct UpdateVisualizationJob : IJobChunk
        {
            public float DeltaTime;
            [ReadOnly] public Core.SimulationConfig Config;
            public ComponentTypeHandle<TerrainData> TerrainHandle;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var terrains = chunk.GetNativeArray(ref TerrainHandle);
                for (int i = 0; i < chunk.Count; i++)
                {
                    var terrain = terrains[i];
                    // Update visualization data
                    terrain.IsDirty = true;
                    terrains[i] = terrain;
                }
            }
        }
    }
} 