using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using ProceduralWorld.Simulation.Components;
using ProceduralWorld.Simulation.Visualization;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ProceduralWorld.Simulation.Core
{
    public class SimulationSceneSetup : MonoBehaviour
    {
        [Header("Terrain Settings")]
        public bool enableTerrainVisualization = true;
        public float worldSize = 500f;
        public int terrainResolution = 256;
        public float heightScale = 20f;
        public float noiseScale = 0.05f;
        public float temperatureScale = 1f;
        public float moistureScale = 1f;
        public int seed = 0;
        public Material terrainMaterial;

        [Header("Resource Settings")]
        public bool enableResourceSystem = true;
        public float resourceDensity = 0.1f;
        public float resourceClustering = 0.5f;

        [Header("Civilization Settings")]
        public bool enableCivilizationSystem = true;
        public int maxCivilizations = 10;
        public float civilizationSpawnRadius = 100f;
        public int civilizationCount = 5;
        public int minCivilizationSize = 100;
        public int maxCivilizationSize = 1000;

        [Header("Religion Settings")]
        public bool enableReligionSystem = true;
        public int maxReligions = 5;
        public float religionSpreadRate = 0.1f;
        public int religionCount = 3;
        public int minReligionSize = 50;
        public int maxReligionSize = 500;

        [Header("Economy Settings")]
        public bool enableEconomySystem = true;
        public float initialWealth = 1000f;
        public float taxRate = 0.1f;

        [Header("History Settings")]
        public bool enableHistorySystem = true;
        public int maxHistoricalEvents = 100;
        public float eventFrequency = 0.1f;
        public int historyResolution = 100;
        public int maxHistoryEntries = 1000;

        [Header("Visualization Settings")]
        public int visualizationMode = 0;
        public int colorMode = 0;
        public bool showResources = true;
        public bool showCivilizations = true;
        public bool showReligions = true;

        [Header("Debug Settings")]
        public bool showDebugInfo = true;
        public bool showPerformanceStats = true;
        public bool showSystemStats = true;

        [Header("Cursor Settings")]
        public bool enableCursorHighlight = true;
        public Color cursorHighlightColor = Color.yellow;
        public float cursorHighlightIntensity = 0.5f;

        [Header("World Naming Settings")]
        public bool enableWorldNaming = true;
        public string worldNamePrefix = "World_";
        public string worldNameSuffix = "";

        private EntityManager EntityManager => World.DefaultGameObjectInjectionWorld.EntityManager;

        private void Awake()
        {
            Debug.Log("[SimulationSceneSetup] Awake called");
            SetupScene();
            SetupCamera();
            SetupLighting();

            // Ensure SimulationSystemGroup exists and is added to the world's update loop
            var world = World.DefaultGameObjectInjectionWorld;
            var simGroup = world.GetExistingSystemManaged<ProceduralWorld.Simulation.Core.SimulationSystemGroup>();
            if (simGroup == null)
            {
                Debug.Log("[SimulationSceneSetup] Creating SimulationSystemGroup and adding to world");
                simGroup = world.CreateSystemManaged<ProceduralWorld.Simulation.Core.SimulationSystemGroup>();
                world.GetOrCreateSystemManaged<Unity.Entities.SimulationSystemGroup>().AddSystemToUpdateList(simGroup);
            }
            else
            {
                Debug.Log("[SimulationSceneSetup] SimulationSystemGroup already exists");
            }
        }

        private void SetupCamera()
        {
            var mainCamera = UnityEngine.Camera.main;
            if (mainCamera == null)
            {
                Debug.Log("[SimulationSceneSetup] Creating main camera");
                var cameraObj = new GameObject("Main Camera");
                mainCamera = cameraObj.AddComponent<UnityEngine.Camera>();
                cameraObj.tag = "MainCamera";
                cameraObj.AddComponent<AudioListener>();
            }

            // Position camera to view the terrain
            float terrainSize = worldSize;
            float cameraHeight = terrainSize * 0.8f; // Increased height for better view
            float cameraDistance = terrainSize * 0.8f; // Increased distance for better view

            mainCamera.transform.position = new Vector3(0, cameraHeight, -cameraDistance);
            mainCamera.transform.rotation = Quaternion.Euler(45, 0, 0);
            
            // Use orthographic camera for better terrain view
            mainCamera.orthographic = true;
            mainCamera.orthographicSize = terrainSize * 0.4f; // Adjust to show full terrain
            mainCamera.nearClipPlane = 0.1f;
            mainCamera.farClipPlane = cameraDistance * 2f;

            // Add Frostpunk-style camera controller
            var cameraController = mainCamera.GetComponent<ProceduralWorld.Simulation.Camera.SimulationCameraController>();
            if (cameraController == null)
            {
                cameraController = mainCamera.gameObject.AddComponent<ProceduralWorld.Simulation.Camera.SimulationCameraController>();
                Debug.Log("[SimulationSceneSetup] Added SimulationCameraController to main camera");
            }
            
            // Add camera focus helper
            var focusHelper = mainCamera.GetComponent<ProceduralWorld.Simulation.Camera.CameraFocusHelper>();
            if (focusHelper == null)
            {
                focusHelper = mainCamera.gameObject.AddComponent<ProceduralWorld.Simulation.Camera.CameraFocusHelper>();
                Debug.Log("[SimulationSceneSetup] Added CameraFocusHelper to main camera");
            }
            
            // Configure camera controller for our world
            cameraController.SetWorldSize(terrainSize);
            cameraController.SetZoom(terrainSize * 0.4f); // Match initial orthographic size
            cameraController.SetPosition(new Vector3(0, cameraHeight, -cameraDistance));

            Debug.Log($"[SimulationSceneSetup] Camera positioned at ({mainCamera.transform.position.x:F2}, {mainCamera.transform.position.y:F2}, {mainCamera.transform.position.z:F2}) with rotation ({mainCamera.transform.rotation.eulerAngles.x:F2}, {mainCamera.transform.rotation.eulerAngles.y:F2}, {mainCamera.transform.rotation.eulerAngles.z:F2})");
            Debug.Log($"[SimulationSceneSetup] Camera controller configured for world size: {terrainSize}");
        }

        private void SetupLighting()
        {
            Debug.Log("[SimulationSceneSetup] Setting up lighting");
            
            // Find or create directional light
            var directionalLight = Object.FindFirstObjectByType<Light>();
            if (directionalLight == null)
            {
                Debug.Log("[SimulationSceneSetup] Creating directional light");
                var lightObj = new GameObject("Directional Light");
                directionalLight = lightObj.AddComponent<Light>();
                directionalLight.type = LightType.Directional;
                directionalLight.intensity = 1.0f;
                directionalLight.shadows = LightShadows.Soft;
                
                // Position and rotate the light
                var lightTransform = directionalLight.transform;
                lightTransform.rotation = Quaternion.Euler(50f, -30f, 0f);
            }
            else
            {
                Debug.Log("[SimulationSceneSetup] Found existing light");
                // Ensure the light is properly configured
                directionalLight.type = LightType.Directional;
                directionalLight.intensity = 1.0f;
                directionalLight.shadows = LightShadows.Soft;
                
                // Position and rotate the light
                var lightTransform = directionalLight.transform;
                lightTransform.rotation = Quaternion.Euler(50f, -30f, 0f);
            }
        }

        private void Start()
        {
            Debug.Log("[SimulationSceneSetup] Start called");
            var world = World.DefaultGameObjectInjectionWorld;
            var entityManager = world.EntityManager;

            // Create terrain data entity if it doesn't exist
            var terrainQuery = entityManager.CreateEntityQuery(typeof(WorldTerrainData));
            Entity terrainDataEntity;
            if (!terrainQuery.HasSingleton<WorldTerrainData>())
            {
                terrainDataEntity = entityManager.CreateEntity();
                entityManager.AddComponent<WorldTerrainData>(terrainDataEntity);
                Debug.Log("[SimulationSceneSetup] Created terrain data entity");
            }
            else
            {
                terrainDataEntity = terrainQuery.GetSingletonEntity();
                Debug.Log("[SimulationSceneSetup] Terrain data entity already exists");
            }
            // Always set the fields:
            var terrainData = new WorldTerrainData
            {
                Resolution = terrainResolution,
                WorldSize = worldSize,
                HeightScale = heightScale,
                NoiseScale = noiseScale,
                Seed = seed
            };
            entityManager.SetComponentData(terrainDataEntity, terrainData);

            // Create visualization state if it doesn't exist
            if (!entityManager.CreateEntityQuery(typeof(VisualizationState)).HasSingleton<VisualizationState>())
            {
                var visualizationStateEntity = entityManager.CreateEntity();
                entityManager.AddComponent<VisualizationState>(visualizationStateEntity);
                var visualizationState = new VisualizationState
                {
                    IsDirty = true
                };
                entityManager.SetComponentData(visualizationStateEntity, visualizationState);
                Debug.Log("[SimulationSceneSetup] Created visualization state");
            }
            else
            {
                Debug.Log("[SimulationSceneSetup] Visualization state already exists");
            }

            // Initialize terrain visualization if enabled
            if (enableTerrainVisualization)
            {
                var visualizationComponent = Object.FindFirstObjectByType<TerrainVisualizationComponent>();
                if (visualizationComponent == null)
                {
                    Debug.Log("[SimulationSceneSetup] Creating TerrainVisualizationComponent");
                    var go = new GameObject("TerrainVisualization");
                    visualizationComponent = go.AddComponent<TerrainVisualizationComponent>();
                    
                    // Set up the component with values from config
                    visualizationComponent.Resolution = terrainResolution;
                    visualizationComponent.WorldSize = worldSize;
                    visualizationComponent.HeightScale = heightScale;
                    visualizationComponent.TemperatureScale = temperatureScale;
                    visualizationComponent.MoistureScale = moistureScale;
                    visualizationComponent.ResourceScale = 1f;
                    visualizationComponent.BiomeScale = 1f;
                    visualizationComponent.Smoothness = 0.5f;
                    visualizationComponent.Metallic = 0f;
                    visualizationComponent.Glossiness = 0.5f;
                    
                    // Initialize the component
                    visualizationComponent.Initialize();
                    
                    // Ensure the object is active in the scene
                    go.SetActive(true);
                }
                else if (!visualizationComponent.IsInitialized)
                {
                    visualizationComponent.Initialize();
                }
            }
        }

        private void OnDestroy()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null) return;

            var entityManager = world.EntityManager;
            var terrainQuery = entityManager.CreateEntityQuery(typeof(WorldTerrainData));

            if (terrainQuery.HasSingleton<WorldTerrainData>())
            {
                var terrainData = terrainQuery.GetSingleton<WorldTerrainData>();
                if (terrainData.HeightMap.IsCreated) terrainData.HeightMap.Dispose();
                if (terrainData.TemperatureMap.IsCreated) terrainData.TemperatureMap.Dispose();
                if (terrainData.MoistureMap.IsCreated) terrainData.MoistureMap.Dispose();
                if (terrainData.BiomeMap.IsCreated) terrainData.BiomeMap.Dispose();
                if (terrainData.ResourceMap.IsCreated) terrainData.ResourceMap.Dispose();
                if (terrainData.ResourceTypeMap.IsCreated) terrainData.ResourceTypeMap.Dispose();
                if (terrainData.GeneratedChunks.IsCreated) terrainData.GeneratedChunks.Dispose();
            }

            terrainQuery.Dispose();
        }

        private void SetupScene()
        {
            Debug.Log("[SimulationSceneSetup] Setting up simulation scene");
            
            // Create SimulationConfig if it doesn't exist
            if (!EntityManager.CreateEntityQuery(typeof(SimulationConfig)).HasSingleton<SimulationConfig>())
            {
                var configEntity = EntityManager.CreateEntity();
                EntityManager.AddComponent<SimulationConfig>(configEntity);
                var config = EntityManager.GetComponentData<SimulationConfig>(configEntity);
                
                // Set default configuration values
                config.EnableTerrainSystem = true;
                config.EnableResourceSystem = true;
                config.EnableCivilizationSystem = true;
                config.EnableCivilizationInteractions = true;
                config.EnableReligionSystem = true;
                config.EnableHistorySystem = true;
                config.EnableVisualizationSystem = true;
                config.EnableDebugVisualization = true;
                config.EnableWorldNamingSystem = true;
                config.EnableCursorSystem = true;
                config.IsSimulationRunning = true;
                config.EnableTerrainGenerationSystem = true;
                config.EnableDebug = true;
                config.EnableHistory = true;
                config.EnableTerrainVisualization = true;
                config.EnableSimulationBootstrap = true;
                config.EnableSimulation = true;
                config.EnableHistoricalEvents = true;
                config.EnableHistoryRecording = true;
                config.EnableCivilizationSpawning = true;
                
                // Set time scale
                config.TimeScale = 1.0f;
                config.CurrentYear = 0;
                
                // Set terrain parameters
                config.WorldSize = (int)worldSize;
                config.TerrainResolution = terrainResolution;
                config.HeightScale = heightScale;
                config.NoiseScale = noiseScale;
                config.TemperatureScale = temperatureScale;
                config.MoistureScale = moistureScale;
                
                // Set resource parameters
                config.ResourceDensity = resourceDensity;
                config.ResourceScale = resourceClustering;
                
                // Set civilization parameters
                config.initialCivilizationPopulation = civilizationCount;
                config.minHeightForCivilization = minCivilizationSize;
                config.maxCivilizationPopulation = maxCivilizationSize;
                
                // Set religion parameters
                config.InitialReligionPopulation = religionCount;
                config.MaxReligionPopulation = maxReligionSize;
                config.minHeightForCivilization = minReligionSize;
                
                // Set history parameters
                config.MaxHistoricalEvents = maxHistoryEntries;
                config.HistoricalEventFrequency = historyResolution;
                
                // Set visualization parameters
                config.VisualizationMode = visualizationMode;
                config.ColorMode = colorMode;
                config.ShowResources = showResources;
                config.ShowCivilizations = showCivilizations;
                config.ShowReligions = showReligions;
                
                // Set debug parameters
                config.ShowDebugInfo = showDebugInfo;
                config.ShowPerformanceStats = showPerformanceStats;
                config.ShowSystemStats = showSystemStats;
                
                // Set cursor parameters
                config.EnableCursorHighlight = enableCursorHighlight;
                config.CursorHighlightColor = new float4(cursorHighlightColor.r, cursorHighlightColor.g, cursorHighlightColor.b, cursorHighlightColor.a);
                config.CursorHighlightIntensity = cursorHighlightIntensity;
                
                // Set world naming parameters
                config.EnableWorldNaming = enableWorldNaming;
                config.WorldNamePrefix = new FixedString64Bytes(worldNamePrefix);
                config.WorldNameSuffix = new FixedString64Bytes(worldNameSuffix);
                
                EntityManager.SetComponentData(configEntity, config);
                Debug.Log("[SimulationSceneSetup] Created SimulationConfig");
            }
            else
            {
                Debug.Log("[SimulationSceneSetup] SimulationConfig already exists");
            }
            
            // Create terrain data entity if it doesn't exist
            var terrainQuery = EntityManager.CreateEntityQuery(typeof(WorldTerrainData));
            Entity terrainDataEntity;
            if (!terrainQuery.HasSingleton<WorldTerrainData>())
            {
                terrainDataEntity = EntityManager.CreateEntity();
                EntityManager.AddComponent<WorldTerrainData>(terrainDataEntity);
                Debug.Log("[SimulationSceneSetup] Created terrain data entity");
            }
            else
            {
                terrainDataEntity = terrainQuery.GetSingletonEntity();
                Debug.Log("[SimulationSceneSetup] Terrain data entity already exists");
            }
            // Always set the fields:
            var terrainData = new WorldTerrainData
            {
                Resolution = terrainResolution,
                WorldSize = worldSize,
                HeightScale = heightScale,
                NoiseScale = noiseScale,
                Seed = seed
            };
            EntityManager.SetComponentData(terrainDataEntity, terrainData);
            
            // Create visualization state if it doesn't exist
            if (!EntityManager.CreateEntityQuery(typeof(VisualizationState)).HasSingleton<VisualizationState>())
            {
                var visualizationStateEntity = EntityManager.CreateEntity();
                EntityManager.AddComponent<VisualizationState>(visualizationStateEntity);
                var visualizationState = EntityManager.GetComponentData<VisualizationState>(visualizationStateEntity);
                visualizationState.IsDirty = true;
                EntityManager.SetComponentData(visualizationStateEntity, visualizationState);
                Debug.Log("[SimulationSceneSetup] Created visualization state");
            }
            else
            {
                Debug.Log("[SimulationSceneSetup] Visualization state already exists");
            }
            
            // Initialize terrain visualization if enabled
            var configQuery = EntityManager.CreateEntityQuery(typeof(SimulationConfig));
            if (configQuery.HasSingleton<SimulationConfig>())
            {
                var config = configQuery.GetSingleton<SimulationConfig>();
                if (config.EnableTerrainVisualization)
                {
                    InitializeTerrainVisualization();
                }
            }
            
            Debug.Log("[SimulationSceneSetup] Scene setup complete");
        }

        private void InitializeTerrainVisualization()
        {
            var visualizationComponent = FindFirstObjectByType<TerrainVisualizationComponent>();
            if (visualizationComponent == null)
            {
                Debug.Log("[SimulationSceneSetup] Creating TerrainVisualizationComponent");
                var go = new GameObject("TerrainVisualization");
                visualizationComponent = go.AddComponent<TerrainVisualizationComponent>();
                
                // Set up the component with values from config
                var config = EntityManager.CreateEntityQuery(typeof(SimulationConfig)).GetSingleton<SimulationConfig>();
                visualizationComponent.Resolution = config.TerrainResolution;
                visualizationComponent.WorldSize = config.WorldSize;
                visualizationComponent.HeightScale = config.HeightScale;
                visualizationComponent.TemperatureScale = config.TemperatureScale;
                visualizationComponent.MoistureScale = config.MoistureScale;
                visualizationComponent.ResourceScale = 1f;
                visualizationComponent.BiomeScale = 1f;
                visualizationComponent.Smoothness = 0.5f;
                visualizationComponent.Metallic = 0f;
                visualizationComponent.Glossiness = 0.5f;
                
                // Initialize the component
                visualizationComponent.Initialize();
                
                // Ensure the object is active in the scene
                go.SetActive(true);
            }
        }

#if UNITY_EDITOR
        [MenuItem("GameObject/Simulation/Setup Scene")]
        public static void CreateSimulationSetup()
        {
            var go = new GameObject("SimulationSceneSetup");
            var setup = go.AddComponent<SimulationSceneSetup>();
            Selection.activeGameObject = go;
            Undo.RegisterCreatedObjectUndo(go, "Create Simulation Scene Setup");
        }

        private void OnValidate()
        {
            if (Application.isPlaying) return;

            // Update terrain visualization if it exists
            var visualizationComponent = Object.FindFirstObjectByType<TerrainVisualizationComponent>();
            if (visualizationComponent != null)
            {
                visualizationComponent.Resolution = terrainResolution;
                visualizationComponent.WorldSize = worldSize;
                visualizationComponent.HeightScale = heightScale;
                
                if (visualizationComponent.IsInitialized)
                {
                    visualizationComponent.Initialize();
                }
            }
        }
#endif
    }
} 