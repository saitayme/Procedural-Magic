using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using ProceduralWorld.Simulation.Components;
using System.Collections.Generic;

namespace ProceduralWorld.Simulation.Visualization
{
    public class LayeredTerrainVisualizer : MonoBehaviour
    {
        [Header("Terrain References")]
        public Terrain terrain;
        public Material terrainMaterial;
        
        [Header("Visualization Modes")]
        public bool showBiomes = true;
        public bool showCivilizations = false;
        public bool showReligions = false;
        public bool showEconomies = false;
        public bool showInfluence = false;
        
        [Header("Layer Settings")]
        [Range(0f, 1f)] public float biomeOpacity = 1f;
        [Range(0f, 1f)] public float civilizationOpacity = 0.7f;
        [Range(0f, 1f)] public float religionOpacity = 0.5f;
        [Range(0f, 1f)] public float economyOpacity = 0.5f;
        [Range(0f, 1f)] public float influenceOpacity = 0.3f;
        
        [Header("Biome Colors")]
        public Color oceanColor = new Color(0.2f, 0.4f, 0.8f, 1f);
        public Color coastColor = new Color(0.8f, 0.8f, 0.6f, 1f);
        public Color plainsColor = new Color(0.4f, 0.7f, 0.3f, 1f);
        public Color forestColor = new Color(0.2f, 0.5f, 0.2f, 1f);
        public Color rainforestColor = new Color(0.1f, 0.4f, 0.1f, 1f);
        public Color mountainColor = new Color(0.6f, 0.6f, 0.6f, 1f);
        public Color desertColor = new Color(0.9f, 0.8f, 0.4f, 1f);
        public Color tundraColor = new Color(0.7f, 0.8f, 0.9f, 1f);
        public Color swampColor = new Color(0.3f, 0.4f, 0.2f, 1f);
        
        [Header("Civilization Colors")]
        public Color[] civilizationColors = {
            Color.red, Color.blue, Color.green, Color.yellow, Color.magenta,
            Color.cyan, new Color(1f, 0.5f, 0f), new Color(0.5f, 0f, 1f),
            new Color(1f, 0.5f, 0.5f), new Color(0.5f, 1f, 0.5f)
        };
        
        [Header("Religion Colors")]
        public Color[] religionColors = {
            new Color(1f, 1f, 0.5f), new Color(0.5f, 1f, 1f), new Color(1f, 0.5f, 1f),
            new Color(0.8f, 0.8f, 0.2f), new Color(0.2f, 0.8f, 0.8f), new Color(0.8f, 0.2f, 0.8f)
        };
        
        [Header("Auto Refresh Settings")]
        [Tooltip("Enable automatic visualization refresh")]
        public bool enableAutoRefresh = true;
        [Tooltip("Refresh mode: Time-based, Data-driven, or Both")]
        public AutoRefreshMode refreshMode = AutoRefreshMode.Both;
        [Tooltip("Time interval between automatic refreshes (in seconds)")]
        [Range(0.1f, 30f)]
        public float autoRefreshInterval = 2f;
        [Tooltip("Detect changes in entity data and refresh automatically")]
        public bool detectDataChanges = true;
        [Tooltip("Still allow manual refresh with R key")]
        public bool allowManualRefresh = true;
        
        public enum AutoRefreshMode
        {
            TimeBased,      // Refresh every X seconds
            DataDriven,     // Refresh when data changes
            Both            // Refresh on timer OR data changes
        }
        
        private EntityManager _entityManager;
        private Texture2D _layeredTexture;
        private bool _isInitialized = false;
        private Dictionary<Entity, int> _civilizationColorMap = new();
        private Dictionary<Entity, int> _religionColorMap = new();
        private TerrainVisualizationComponent _terrainVisualization;
        
        // Auto-refresh tracking variables
        private float _lastRefreshTime = 0f;
        private int _lastCivilizationCount = 0;
        private int _lastReligionCount = 0;
        private int _lastEconomyCount = 0;

        void Start()
        {
            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            
            // Try to find Unity Terrain first
            if (terrain == null)
                terrain = FindFirstObjectByType<Terrain>();
                
            // If no Unity Terrain, look for TerrainVisualizationComponent
            if (terrain == null)
            {
                _terrainVisualization = FindFirstObjectByType<TerrainVisualizationComponent>();
                if (_terrainVisualization != null)
                {
                    var meshRenderer = _terrainVisualization.GetComponent<MeshRenderer>();
                    if (meshRenderer != null)
                        terrainMaterial = meshRenderer.material;
                }
            }
            else if (terrainMaterial == null && terrain != null)
            {
                terrainMaterial = terrain.materialTemplate;
            }
            
            Debug.Log($"[LayeredTerrainVisualizer] Initialized - Unity Terrain: {terrain != null}, TerrainVisualization: {_terrainVisualization != null}, Material: {terrainMaterial != null}");
        }

        void Update()
        {
            if (_entityManager == null) return;
            
            // Check if we have either terrain type
            bool hasTerrainTarget = (terrain != null) || (_terrainVisualization != null);
            if (!hasTerrainTarget) return;
            
            bool shouldRefresh = false;
            
            // Check for initial setup
            if (!_isInitialized)
            {
                shouldRefresh = true;
            }
            // Check for manual refresh (if enabled)
            else if (allowManualRefresh && Input.GetKeyDown(KeyCode.R))
            {
                shouldRefresh = true;
                Debug.Log("[LayeredTerrainVisualizer] Manual refresh triggered");
            }
            // Check for auto-refresh (if enabled)
            else if (enableAutoRefresh)
            {
                bool timeBasedRefresh = false;
                bool dataBasedRefresh = false;
                
                // Time-based refresh check
                if (refreshMode == AutoRefreshMode.TimeBased || refreshMode == AutoRefreshMode.Both)
                {
                    if (Time.time - _lastRefreshTime >= autoRefreshInterval)
                    {
                        timeBasedRefresh = true;
                    }
                }
                
                // Data-driven refresh check
                if (detectDataChanges && (refreshMode == AutoRefreshMode.DataDriven || refreshMode == AutoRefreshMode.Both))
                {
                    dataBasedRefresh = CheckForDataChanges();
                }
                
                shouldRefresh = timeBasedRefresh || dataBasedRefresh;
                
                if (shouldRefresh)
                {
                    string refreshReason = timeBasedRefresh ? "time-based" : "data-driven";
                    Debug.Log($"[LayeredTerrainVisualizer] Auto-refresh triggered ({refreshReason})");
                }
            }
            
            if (shouldRefresh)
            {
                GenerateLayeredVisualization();
                _lastRefreshTime = Time.time;
            }
        }
        
        private bool CheckForDataChanges()
        {
            // Quick check for entity count changes
            var civQuery = _entityManager.CreateEntityQuery(typeof(CivilizationData));
            var religionQuery = _entityManager.CreateEntityQuery(typeof(ReligionData));
            var economyQuery = _entityManager.CreateEntityQuery(typeof(EconomyData));
            
            int currentCivCount = civQuery.CalculateEntityCount();
            int currentReligionCount = religionQuery.CalculateEntityCount();
            int currentEconomyCount = economyQuery.CalculateEntityCount();
            
            bool hasChanged = (currentCivCount != _lastCivilizationCount) ||
                             (currentReligionCount != _lastReligionCount) ||
                             (currentEconomyCount != _lastEconomyCount);
            
            if (hasChanged)
            {
                _lastCivilizationCount = currentCivCount;
                _lastReligionCount = currentReligionCount;
                _lastEconomyCount = currentEconomyCount;
                return true;
            }
            
            return false;
        }

        public void GenerateLayeredVisualization()
        {
            if (_entityManager == null)
            {
                Debug.LogWarning("[LayeredTerrainVisualizer] EntityManager is null");
                return;
            }

            var terrainQuery = _entityManager.CreateEntityQuery(typeof(WorldTerrainData));
            if (!terrainQuery.HasSingleton<WorldTerrainData>()) 
            {
                Debug.LogWarning("[LayeredTerrainVisualizer] No WorldTerrainData found");
                return;
            }
            
            var terrainData = _entityManager.GetComponentData<WorldTerrainData>(terrainQuery.GetSingletonEntity());
            
            if (!terrainData.BiomeMap.IsCreated || terrainData.BiomeMap.Length == 0)
            {
                Debug.Log("[LayeredTerrainVisualizer] BiomeMap not initialized yet");
                return;
            }

            int resolution = terrainData.Resolution;
            Debug.Log($"[LayeredTerrainVisualizer] Starting generation with resolution {resolution}x{resolution}");
            
            // Create layered texture
            if (_layeredTexture != null)
                DestroyImmediate(_layeredTexture);
                
            _layeredTexture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
            
            var pixels = new Color[resolution * resolution];
            
            // Get entity data for layering
            var civilizations = GetCivilizationData();
            var religions = GetReligionData();
            var economies = GetEconomyData();
            
            Debug.Log($"[LayeredTerrainVisualizer] Found {civilizations.Count} civilizations, {religions.Count} religions, {economies.Count} economies");
            
            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    int index = y * resolution + x;
                    Vector3 worldPos = GetWorldPosition(x, y, terrainData);
                    
                    Color finalColor = Color.black;
                    
                    // Layer 1: Biomes (base layer)
                    if (showBiomes)
                    {
                        var biome = terrainData.BiomeMap[index];
                        Color biomeColor = GetBiomeColor(biome);
                        finalColor = Color.Lerp(finalColor, biomeColor, biomeOpacity);
                    }
                    
                    // Layer 2: Civilizations
                    if (showCivilizations)
                    {
                        var civColor = GetCivilizationColorAtPosition(worldPos, civilizations);
                        if (civColor.a > 0)
                        {
                            finalColor = Color.Lerp(finalColor, civColor, civilizationOpacity * civColor.a);
                        }
                    }
                    
                    // Layer 3: Religions
                    if (showReligions)
                    {
                        var religionColor = GetReligionColorAtPosition(worldPos, religions);
                        if (religionColor.a > 0)
                        {
                            finalColor = Color.Lerp(finalColor, religionColor, religionOpacity * religionColor.a);
                        }
                    }
                    
                    // Layer 4: Economies
                    if (showEconomies)
                    {
                        var economyColor = GetEconomyColorAtPosition(worldPos, economies);
                        if (economyColor.a > 0)
                        {
                            finalColor = Color.Lerp(finalColor, economyColor, economyOpacity * economyColor.a);
                        }
                    }
                    
                    pixels[index] = finalColor;
                }
            }
            
            _layeredTexture.SetPixels(pixels);
            _layeredTexture.Apply();
            
            // Apply to terrain material
            if (terrainMaterial != null)
            {
                terrainMaterial.mainTexture = _layeredTexture;
                Debug.Log("[LayeredTerrainVisualizer] Applied texture to existing material");
            }
            else
            {
                // Create new material
                terrainMaterial = new Material(Shader.Find("Standard"));
                terrainMaterial.mainTexture = _layeredTexture;
                
                // Apply to Unity Terrain if available
                if (terrain != null && terrain.materialTemplate == null)
                {
                    terrain.materialTemplate = terrainMaterial;
                    Debug.Log("[LayeredTerrainVisualizer] Applied new material to Unity Terrain");
                }
                // Apply to TerrainVisualizationComponent if available
                else if (_terrainVisualization != null)
                {
                    var meshRenderer = _terrainVisualization.GetComponent<MeshRenderer>();
                    if (meshRenderer != null)
                    {
                        meshRenderer.material = terrainMaterial;
                        Debug.Log("[LayeredTerrainVisualizer] Applied new material to TerrainVisualizationComponent");
                    }
                }
            }
            
            _isInitialized = true;
            Debug.Log($"[LayeredTerrainVisualizer] Generated {resolution}x{resolution} layered visualization successfully");
        }

        Vector3 GetWorldPosition(int x, int y, WorldTerrainData terrainData)
        {
            float worldSize = terrainData.WorldSize;
            float scaleX = worldSize / (terrainData.Resolution - 1);
            float scaleZ = worldSize / (terrainData.Resolution - 1);
            
            float xPos = (x * scaleX) - (worldSize * 0.5f);
            float zPos = (y * scaleZ) - (worldSize * 0.5f);
            
            return new Vector3(xPos, 0, zPos);
        }

        List<CivilizationData> GetCivilizationData()
        {
            var civQuery = _entityManager.CreateEntityQuery(typeof(CivilizationData));
            var civEntities = civQuery.ToEntityArray(Allocator.Temp);
            var civData = civQuery.ToComponentDataArray<CivilizationData>(Allocator.Temp);
            
            var result = new List<CivilizationData>();
            for (int i = 0; i < civData.Length; i++)
            {
                result.Add(civData[i]);
                
                // Assign color indices
                if (!_civilizationColorMap.ContainsKey(civEntities[i]))
                {
                    _civilizationColorMap[civEntities[i]] = _civilizationColorMap.Count % civilizationColors.Length;
                }
            }
            
            civEntities.Dispose();
            civData.Dispose();
            return result;
        }

        List<ReligionData> GetReligionData()
        {
            var religionQuery = _entityManager.CreateEntityQuery(typeof(ReligionData));
            var religionEntities = religionQuery.ToEntityArray(Allocator.Temp);
            var religionData = religionQuery.ToComponentDataArray<ReligionData>(Allocator.Temp);
            
            var result = new List<ReligionData>();
            for (int i = 0; i < religionData.Length; i++)
            {
                result.Add(religionData[i]);
                
                // Assign color indices
                if (!_religionColorMap.ContainsKey(religionEntities[i]))
                {
                    _religionColorMap[religionEntities[i]] = _religionColorMap.Count % religionColors.Length;
                }
            }
            
            religionEntities.Dispose();
            religionData.Dispose();
            return result;
        }

        List<EconomyData> GetEconomyData()
        {
            var economyQuery = _entityManager.CreateEntityQuery(typeof(EconomyData));
            var economyData = economyQuery.ToComponentDataArray<EconomyData>(Allocator.Temp);
            
            var result = new List<EconomyData>();
            for (int i = 0; i < economyData.Length; i++)
            {
                result.Add(economyData[i]);
            }
            
            economyData.Dispose();
            return result;
        }

        Color GetCivilizationColorAtPosition(Vector3 worldPos, List<CivilizationData> civilizations)
        {
            float minDistance = float.MaxValue;
            int closestCivIndex = -1;
            
            for (int i = 0; i < civilizations.Count; i++)
            {
                var civ = civilizations[i];
                float distance = Vector3.Distance(worldPos, new Vector3(civ.Position.x, 0, civ.Position.z));
                
                // Influence radius based on civilization size and influence
                float influenceRadius = 50f + (civ.Population * 0.1f) + (civ.Influence * 30f);
                
                if (distance < influenceRadius && distance < minDistance)
                {
                    minDistance = distance;
                    closestCivIndex = i;
                }
            }
            
            if (closestCivIndex >= 0)
            {
                var colorIndex = closestCivIndex % civilizationColors.Length;
                var color = civilizationColors[colorIndex];
                
                // Fade based on distance
                float fadeDistance = 50f + (civilizations[closestCivIndex].Population * 0.1f);
                float alpha = 1f - (minDistance / fadeDistance);
                color.a = Mathf.Clamp01(alpha);
                
                return color;
            }
            
            return Color.clear;
        }

        Color GetReligionColorAtPosition(Vector3 worldPos, List<ReligionData> religions)
        {
            float minDistance = float.MaxValue;
            int closestReligionIndex = -1;
            
            for (int i = 0; i < religions.Count; i++)
            {
                var religion = religions[i];
                float distance = Vector3.Distance(worldPos, new Vector3(religion.Position.x, 0, religion.Position.z));
                
                // Influence radius based on religion influence and followers
                float influenceRadius = 30f + (religion.Influence * 40f) + (religion.FollowerCount * 0.05f);
                
                if (distance < influenceRadius && distance < minDistance)
                {
                    minDistance = distance;
                    closestReligionIndex = i;
                }
            }
            
            if (closestReligionIndex >= 0)
            {
                var colorIndex = closestReligionIndex % religionColors.Length;
                var color = religionColors[colorIndex];
                
                // Fade based on distance
                float fadeDistance = 30f + (religions[closestReligionIndex].Influence * 40f);
                float alpha = 1f - (minDistance / fadeDistance);
                color.a = Mathf.Clamp01(alpha);
                
                return color;
            }
            
            return Color.clear;
        }

        Color GetEconomyColorAtPosition(Vector3 worldPos, List<EconomyData> economies)
        {
            float minDistance = float.MaxValue;
            int closestEconomyIndex = -1;
            
            for (int i = 0; i < economies.Count; i++)
            {
                var economy = economies[i];
                float distance = Vector3.Distance(worldPos, new Vector3(economy.Location.x, 0, economy.Location.z));
                
                // Influence radius based on economy wealth and trade
                float influenceRadius = 25f + (economy.Wealth * 0.01f) + (economy.Trade * 20f);
                
                if (distance < influenceRadius && distance < minDistance)
                {
                    minDistance = distance;
                    closestEconomyIndex = i;
                }
            }
            
            if (closestEconomyIndex >= 0)
            {
                // Use a golden color for economies
                var color = new Color(1f, 0.8f, 0.2f, 1f);
                
                // Fade based on distance
                float fadeDistance = 25f + (economies[closestEconomyIndex].Wealth * 0.01f);
                float alpha = 1f - (minDistance / fadeDistance);
                color.a = Mathf.Clamp01(alpha);
                
                return color;
            }
            
            return Color.clear;
        }

        Color GetBiomeColor(ProceduralWorld.Simulation.Core.BiomeType biome)
        {
            switch (biome)
            {
                case ProceduralWorld.Simulation.Core.BiomeType.Ocean:
                    return oceanColor;
                case ProceduralWorld.Simulation.Core.BiomeType.Coast:
                    return coastColor;
                case ProceduralWorld.Simulation.Core.BiomeType.Plains:
                    return plainsColor;
                case ProceduralWorld.Simulation.Core.BiomeType.Forest:
                    return forestColor;
                case ProceduralWorld.Simulation.Core.BiomeType.Rainforest:
                    return rainforestColor;
                case ProceduralWorld.Simulation.Core.BiomeType.Mountains:
                    return mountainColor;
                case ProceduralWorld.Simulation.Core.BiomeType.Desert:
                    return desertColor;
                case ProceduralWorld.Simulation.Core.BiomeType.Tundra:
                    return tundraColor;
                case ProceduralWorld.Simulation.Core.BiomeType.Swamp:
                    return swampColor;
                default:
                    return Color.magenta; // Debug color for unknown biomes
            }
        }

        void OnDestroy()
        {
            if (_layeredTexture != null)
            {
                DestroyImmediate(_layeredTexture);
            }
        }

        // UI Controls - DISABLED: Now managed by SimulationUIManager
        /*
        void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 350, 500));
            GUILayout.Label("Terrain Visualization Controls");
            
            showBiomes = GUILayout.Toggle(showBiomes, "Show Biomes");
            if (showBiomes)
                biomeOpacity = GUILayout.HorizontalSlider(biomeOpacity, 0f, 1f);
                
            showCivilizations = GUILayout.Toggle(showCivilizations, "Show Civilizations");
            if (showCivilizations)
                civilizationOpacity = GUILayout.HorizontalSlider(civilizationOpacity, 0f, 1f);
                
            showReligions = GUILayout.Toggle(showReligions, "Show Religions");
            if (showReligions)
                religionOpacity = GUILayout.HorizontalSlider(religionOpacity, 0f, 1f);
                
            showEconomies = GUILayout.Toggle(showEconomies, "Show Economies");
            if (showEconomies)
                economyOpacity = GUILayout.HorizontalSlider(economyOpacity, 0f, 1f);
            
            GUILayout.Space(10);
            GUILayout.Label("Auto-Refresh Settings");
            
            enableAutoRefresh = GUILayout.Toggle(enableAutoRefresh, "Enable Auto-Refresh");
            if (enableAutoRefresh)
            {
                refreshMode = (AutoRefreshMode)GUILayout.SelectionGrid((int)refreshMode, 
                    new string[] { "Time-Based", "Data-Driven", "Both" }, 3);
                
                GUILayout.Label($"Refresh Interval: {autoRefreshInterval:F1}s");
                autoRefreshInterval = GUILayout.HorizontalSlider(autoRefreshInterval, 0.1f, 30f);
                
                detectDataChanges = GUILayout.Toggle(detectDataChanges, "Detect Data Changes");
                
                // Show auto-refresh status
                float timeSinceLastRefresh = Time.time - _lastRefreshTime;
                string status = enableAutoRefresh ? 
                    $"Next refresh in: {Mathf.Max(0, autoRefreshInterval - timeSinceLastRefresh):F1}s" : 
                    "Auto-refresh disabled";
                GUILayout.Label(status);
            }
            
            allowManualRefresh = GUILayout.Toggle(allowManualRefresh, "Allow Manual Refresh (R key)");
            
            if (allowManualRefresh && GUILayout.Button("Manual Refresh (R)"))
            {
                GenerateLayeredVisualization();
                _lastRefreshTime = Time.time;
            }
            
            // Show entity counts
            GUILayout.Space(10);
            GUILayout.Label("Entity Counts:");
            GUILayout.Label($"Civilizations: {_lastCivilizationCount}");
            GUILayout.Label($"Religions: {_lastReligionCount}");
            GUILayout.Label($"Economies: {_lastEconomyCount}");
            
            GUILayout.EndArea();
        }
        */
    }
} 