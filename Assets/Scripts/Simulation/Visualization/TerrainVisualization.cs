using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;
using Unity.Jobs;
using Unity.Burst.Intrinsics;
using UnityEngine;
using UnityEngine.SceneManagement;
using ProceduralWorld.Simulation.Core;
using ProceduralWorld.Simulation.Components;
using ProceduralWorld.Simulation.Utils;
using ProceduralWorld.Simulation.Systems;

namespace ProceduralWorld.Simulation.Visualization
{
    [UpdateInGroup(typeof(ProceduralWorld.Simulation.Core.SimulationSystemGroup))]
    [UpdateAfter(typeof(ProceduralWorld.Simulation.Systems.TerrainGenerationSystem))]
    public partial class TerrainVisualizationSystem : SystemBase
    {
        private EntityQuery _terrainDataQuery;
        private EntityQuery _visualizationStateQuery;
        private TerrainVisualizationComponent _visualizationComponent;

        protected override void OnCreate()
        {
            Debug.Log("[TerrainVisualizationSystem] System created");
            RequireForUpdate<WorldTerrainData>();
            RequireForUpdate<ProceduralWorld.Simulation.Core.VisualizationState>();

            _terrainDataQuery = SystemAPI.QueryBuilder()
                .WithAll<WorldTerrainData>()
                .Build();

            _visualizationStateQuery = SystemAPI.QueryBuilder()
                .WithAll<ProceduralWorld.Simulation.Core.VisualizationState>()
                .Build();
        }

        protected override void OnStartRunning()
        {
            Debug.Log("[TerrainVisualizationSystem] Starting system");
            
            // Find or create the visualization component
            _visualizationComponent = Object.FindFirstObjectByType<TerrainVisualizationComponent>();
            if (_visualizationComponent == null)
            {
                Debug.Log("[TerrainVisualizationSystem] Creating new TerrainVisualizationComponent");
                var go = new GameObject("TerrainVisualization");
                _visualizationComponent = go.AddComponent<TerrainVisualizationComponent>();
                
                // Get configuration from SimulationConfig
                var config = SystemAPI.GetSingleton<SimulationConfig>();
                
                // Set up the component with values from config
                _visualizationComponent.Resolution = config.TerrainResolution;
                _visualizationComponent.WorldSize = config.WorldSize;
                _visualizationComponent.HeightScale = config.HeightScale;
                _visualizationComponent.TemperatureScale = 1f;
                _visualizationComponent.MoistureScale = 1f;
                _visualizationComponent.ResourceScale = 1f;
                _visualizationComponent.BiomeScale = 1f;
                _visualizationComponent.Smoothness = 0.5f;
                _visualizationComponent.Metallic = 0f;
                _visualizationComponent.Glossiness = 0.5f;
                
                // Initialize the component
                _visualizationComponent.Initialize();
                
                // Ensure the object is active in the scene
                go.SetActive(true);
                
                // Add the GameObject to the scene
                SceneManager.MoveGameObjectToScene(go, SceneManager.GetActiveScene());
            }
            else
            {
                Debug.Log("[TerrainVisualizationSystem] Found existing TerrainVisualizationComponent");
                
                // Update configuration from SimulationConfig
                var config = SystemAPI.GetSingleton<SimulationConfig>();
                _visualizationComponent.Resolution = config.TerrainResolution;
                _visualizationComponent.WorldSize = config.WorldSize;
                _visualizationComponent.HeightScale = config.HeightScale;
                
                if (!_visualizationComponent.IsInitialized)
                {
                    _visualizationComponent.Initialize();
                }
                // Ensure the object is active
                _visualizationComponent.gameObject.SetActive(true);
            }
        }

        protected override void OnUpdate()
        {
            if (!_terrainDataQuery.HasSingleton<WorldTerrainData>())
            {
                Debug.LogWarning("[TerrainVisualizationSystem] No terrain data entity found");
                return;
            }

            var terrainData = SystemAPI.GetSingleton<WorldTerrainData>();
            var visualizationState = SystemAPI.GetSingleton<ProceduralWorld.Simulation.Core.VisualizationState>();

            if (!visualizationState.IsDirty)
            {
                return;
            }

            Debug.Log("[TerrainVisualizationSystem] Updating terrain visualization");

            try
            {
                if (_visualizationComponent == null)
                {
                    Debug.LogError("[TerrainVisualizationSystem] Visualization component is null");
                    return;
                }

                // Update the visualization component with the terrain data
                _visualizationComponent.UpdateTerrainMesh(
                    terrainData.HeightMap,
                    terrainData.TemperatureMap,
                    terrainData.MoistureMap,
                    terrainData.BiomeMap,
                    terrainData.ResourceMap,
                    terrainData.ResourceTypeMap
                );

                // Mark visualization as clean
                visualizationState.IsDirty = false;
                SystemAPI.SetSingleton(visualizationState);
                Debug.Log("[TerrainVisualizationSystem] Terrain visualization updated successfully");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[TerrainVisualizationSystem] Error updating visualization: {e.Message}\n{e.StackTrace}");
            }
        }

        protected override void OnDestroy()
        {
            if (_visualizationComponent != null)
            {
                Object.Destroy(_visualizationComponent.gameObject);
            }
        }
    }
} 