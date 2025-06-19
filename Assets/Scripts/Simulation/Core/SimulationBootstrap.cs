using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;
using Unity.Jobs;
using UnityEngine;
using ProceduralWorld.Simulation.Components;
using ProceduralWorld.Simulation.Utils;
using ProceduralWorld.Simulation.Core;
using ProceduralWorld.Simulation.Visualization;

namespace ProceduralWorld.Simulation.Core
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [BurstCompile]
    public partial struct SimulationBootstrap : ISystem
    {
        private bool _isInitialized;
        private Entity _worldDataEntity;
        private Entity _visualizationEntity;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _isInitialized = false;
            _worldDataEntity = Entity.Null;
            _visualizationEntity = Entity.Null;

            state.RequireForUpdate<SimulationConfig>();
            Debug.Log("[SimulationBootstrap] System created");
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            if (_worldDataEntity != Entity.Null)
            {
                if (state.EntityManager.HasComponent<WorldTerrainData>(_worldDataEntity))
                {
                    var terrainData = state.EntityManager.GetComponentData<WorldTerrainData>(_worldDataEntity);
                    if (terrainData.HeightMap.IsCreated) terrainData.HeightMap.Dispose();
                    if (terrainData.TemperatureMap.IsCreated) terrainData.TemperatureMap.Dispose();
                    if (terrainData.MoistureMap.IsCreated) terrainData.MoistureMap.Dispose();
                    if (terrainData.BiomeMap.IsCreated) terrainData.BiomeMap.Dispose();
                    if (terrainData.ResourceMap.IsCreated) terrainData.ResourceMap.Dispose();
                    if (terrainData.ResourceTypeMap.IsCreated) terrainData.ResourceTypeMap.Dispose();
                    if (terrainData.GeneratedChunks.IsCreated) terrainData.GeneratedChunks.Dispose();
                }
            }

            Debug.Log("[SimulationBootstrap] System destroyed");
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (_isInitialized)
                return;

            Debug.Log("[SimulationBootstrap] Initializing simulation");

            // Create world data entity if it doesn't exist
            if (!SystemAPI.HasSingleton<WorldData>())
            {
                var worldDataEntity = state.EntityManager.CreateEntity();
                state.EntityManager.AddComponent<WorldData>(worldDataEntity);
                var worldData = state.EntityManager.GetComponentData<WorldData>(worldDataEntity);
                worldData.SimulationTime = 0f;
                worldData.SimulationSpeed = 1f;
                state.EntityManager.SetComponentData(worldDataEntity, worldData);
                Debug.Log("[SimulationBootstrap] Created world data entity");
            }
            else
            {
                Debug.Log("[SimulationBootstrap] World data entity already exists");
            }

            // Create visualization state entity if it doesn't exist
            if (!SystemAPI.HasSingleton<VisualizationState>())
            {
                var visualizationEntity = state.EntityManager.CreateEntity();
                state.EntityManager.AddComponent<VisualizationState>(visualizationEntity);
                var visualizationState = state.EntityManager.GetComponentData<VisualizationState>(visualizationEntity);
                visualizationState.IsDirty = true;
                state.EntityManager.SetComponentData(visualizationEntity, visualizationState);
                Debug.Log("[SimulationBootstrap] Created visualization state");
            }
            else
            {
                Debug.Log("[SimulationBootstrap] Visualization state already exists");
            }

            // Create terrain data if it doesn't exist
            if (!SystemAPI.HasSingleton<WorldTerrainData>())
            {
                var terrainDataEntity = state.EntityManager.CreateEntity();
                state.EntityManager.AddComponent<WorldTerrainData>(terrainDataEntity);
                Debug.Log("[SimulationBootstrap] Created terrain data entity");
            }
            else
            {
                Debug.Log("[SimulationBootstrap] Terrain data entity already exists");
            }

            _isInitialized = true;
            Debug.Log("[SimulationBootstrap] Initialization complete");
        }
    }
} 