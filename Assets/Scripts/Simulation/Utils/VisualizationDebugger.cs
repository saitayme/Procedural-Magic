using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using ProceduralWorld.Simulation.Components;
using ProceduralWorld.Simulation.Visualization;
using ProceduralWorld.Simulation.Core;
using ProceduralWorld.Simulation.Systems;
using CoreBiomeType = ProceduralWorld.Simulation.Core.BiomeType;
using System.Collections.Generic;
using UnityEditor;

namespace ProceduralWorld.Simulation.Utils
{
    public class VisualizationDebugger : MonoBehaviour
    {
        [Header("Debug Settings")]
        public bool autoStart = true;
        public bool continuousDebug = false;
        public bool showDebugGUI = true;
        public float debugInterval = 2f;
        
        private float lastDebugTime;
        private EntityManager _entityManager;
        
        // Cached UI data to prevent flickering
        private const float UI_UPDATE_INTERVAL = 2f; // Update UI every 2 seconds
        private List<CivilizationData> _cachedCivilizations = new List<CivilizationData>();
        private List<HistoricalEventRecord> _cachedEvents = new List<HistoricalEventRecord>();
        private List<TerritoryData> _cachedTerritories = new List<TerritoryData>();
        
        // Scroll positions for UI
        private Vector2 _civilizationScrollPos = Vector2.zero;
        private Vector2 _eventsScrollPos = Vector2.zero;
        private Vector2 _territoriesScrollPos = Vector2.zero;
        
        // Reference to history system
        private WorldHistorySystem _historySystem;
        
        void Start()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world != null)
            {
                _entityManager = world.EntityManager;
                _historySystem = world.GetOrCreateSystemManaged<WorldHistorySystem>(); // Get history system reference
            }
            
            if (autoStart)
            {
                DebugVisualizationSystem();
            }
        }
        
        void Update()
        {
            // Toggle debug GUI with F1 key
            if (Input.GetKeyDown(KeyCode.F1))
            {
                showDebugGUI = !showDebugGUI;
            }
            
            if (continuousDebug && Time.time - lastDebugTime > debugInterval)
            {
                DebugVisualizationSystem();
                lastDebugTime = Time.time;
            }
        }
        
        [ContextMenu("Debug Visualization System")]
        public void DebugVisualizationSystem()
        {
            Debug.Log("=== VISUALIZATION DEBUG REPORT ===");
            
            // 1. Check Entity Manager
            DebugEntityManager();
            
            // 2. Check Terrain Data
            DebugTerrainData();
            
            // 3. Check Visualization Components
            DebugVisualizationComponents();
            
            // 4. Check Scene Objects
            DebugSceneObjects();
            
            // 5. Check Materials and Textures
            DebugMaterialsAndTextures();
            
            Debug.Log("=== END DEBUG REPORT ===");
        }
        
        void DebugEntityManager()
        {
            Debug.Log("--- Entity Manager Debug ---");
            
            if (_entityManager == null)
            {
                Debug.LogError("EntityManager is NULL! ECS World might not be initialized.");
                return;
            }
            
            Debug.Log($"EntityManager exists: {_entityManager != null}");
            
            // Check for WorldTerrainData
            var terrainQuery = _entityManager.CreateEntityQuery(typeof(WorldTerrainData));
            Debug.Log($"WorldTerrainData entities found: {terrainQuery.CalculateEntityCount()}");
            
            if (terrainQuery.HasSingleton<WorldTerrainData>())
            {
                var terrainData = _entityManager.GetComponentData<WorldTerrainData>(terrainQuery.GetSingletonEntity());
                Debug.Log($"TerrainData - Resolution: {terrainData.Resolution}, WorldSize: {terrainData.WorldSize}");
                Debug.Log($"BiomeMap created: {terrainData.BiomeMap.IsCreated}, Length: {(terrainData.BiomeMap.IsCreated ? terrainData.BiomeMap.Length : 0)}");
                Debug.Log($"HeightMap created: {terrainData.HeightMap.IsCreated}, Length: {(terrainData.HeightMap.IsCreated ? terrainData.HeightMap.Length : 0)}");
                
                // Sample some biome data
                if (terrainData.BiomeMap.IsCreated && terrainData.BiomeMap.Length > 0)
                {
                    var biomeCounts = new System.Collections.Generic.Dictionary<CoreBiomeType, int>();
                    for (int i = 0; i < terrainData.BiomeMap.Length; i++)
                    {
                        var biome = terrainData.BiomeMap[i];
                        if (!biomeCounts.ContainsKey(biome))
                            biomeCounts[biome] = 0;
                        biomeCounts[biome]++;
                    }
                    
                    Debug.Log("Biome distribution:");
                    foreach (var kvp in biomeCounts)
                    {
                        Debug.Log($"  {kvp.Key}: {kvp.Value} tiles ({(kvp.Value * 100f / terrainData.BiomeMap.Length):F1}%)");
                    }
                }
            }
            else
            {
                Debug.LogWarning("No WorldTerrainData singleton found!");
            }
            
            // Check other entity types
            var civQuery = _entityManager.CreateEntityQuery(typeof(CivilizationData));
            Debug.Log($"Civilization entities: {civQuery.CalculateEntityCount()}");
            
            var religionQuery = _entityManager.CreateEntityQuery(typeof(ReligionData));
            Debug.Log($"Religion entities: {religionQuery.CalculateEntityCount()}");
            
            var economyQuery = _entityManager.CreateEntityQuery(typeof(EconomyData));
            Debug.Log($"Economy entities: {economyQuery.CalculateEntityCount()}");
            
            var territoryQuery = _entityManager.CreateEntityQuery(typeof(TerritoryData));
            Debug.Log($"Territory entities: {territoryQuery.CalculateEntityCount()}");
        }
        
        void DebugTerrainData()
        {
            Debug.Log("--- Terrain Data Debug ---");
            
            var terrain = FindFirstObjectByType<Terrain>();
            if (terrain != null)
            {
                Debug.Log($"Unity Terrain found: {terrain.name}");
                Debug.Log($"Terrain position: {terrain.transform.position}");
                Debug.Log($"Terrain data: {(terrain.terrainData != null ? "EXISTS" : "NULL")}");
                Debug.Log($"Terrain material: {(terrain.materialTemplate != null ? terrain.materialTemplate.name : "NULL")}");
                
                if (terrain.terrainData != null)
                {
                    Debug.Log($"Terrain size: {terrain.terrainData.size}");
                    Debug.Log($"Heightmap resolution: {terrain.terrainData.heightmapResolution}");
                }
            }
            // Note: No warning for custom mesh-based terrain systems
            
            // Check TerrainVisualizationComponent
            var terrainVis = FindFirstObjectByType<TerrainVisualizationComponent>();
            if (terrainVis != null)
            {
                Debug.Log($"TerrainVisualizationComponent found: {terrainVis.name}");
                Debug.Log($"Is initialized: {terrainVis.IsInitialized}");
                Debug.Log($"Resolution: {terrainVis.Resolution}, WorldSize: {terrainVis.WorldSize}");
            }
            else
            {
                Debug.Log("No TerrainVisualizationComponent found");
            }
        }
        
        void DebugVisualizationComponents()
        {
            Debug.Log("--- Visualization Components Debug ---");
            
            // LayeredTerrainVisualizer
            var layeredVis = FindFirstObjectByType<LayeredTerrainVisualizer>();
            if (layeredVis != null)
            {
                Debug.Log($"LayeredTerrainVisualizer found: {layeredVis.name}");
                Debug.Log($"GameObject active: {layeredVis.gameObject.activeInHierarchy}");
                Debug.Log($"Component enabled: {layeredVis.enabled}");
                Debug.Log($"Assigned terrain: {(layeredVis.terrain != null ? layeredVis.terrain.name : "NULL")}");
                Debug.Log($"Assigned material: {(layeredVis.terrainMaterial != null ? layeredVis.terrainMaterial.name : "NULL")}");
                Debug.Log($"Show biomes: {layeredVis.showBiomes}");
                Debug.Log($"Show civilizations: {layeredVis.showCivilizations}");
            }
            else
            {
                Debug.LogError("LayeredTerrainVisualizer NOT FOUND!");
            }
            
            // BiomeLabelVisualizer
            var biomeLabels = FindObjectsByType<BiomeLabelVisualizer>(FindObjectsSortMode.None);
            Debug.Log($"BiomeLabelVisualizer count: {biomeLabels.Length}");
            
            // EntityMarkerVisualizer
            var entityMarkers = FindObjectsByType<EntityMarkerVisualizer>(FindObjectsSortMode.None);
            Debug.Log($"EntityMarkerVisualizer count: {entityMarkers.Length}");
            
            // Note: TerrainColorVisualizer is obsolete - use LayeredTerrainVisualizer instead
            Debug.Log("TerrainColorVisualizer is deprecated - using LayeredTerrainVisualizer instead");
        }
        
        void DebugSceneObjects()
        {
            Debug.Log("--- Scene Objects Debug ---");
            
            var allGameObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            Debug.Log($"Total GameObjects in scene: {allGameObjects.Length}");
            
            // Look for objects with "Terrain" or "Visualization" in name
            foreach (var go in allGameObjects)
            {
                if (go.name.ToLower().Contains("terrain") || go.name.ToLower().Contains("visual"))
                {
                    Debug.Log($"Found relevant object: {go.name} (active: {go.activeInHierarchy})");
                    var components = go.GetComponents<Component>();
                    foreach (var comp in components)
                    {
                        if (comp != null)
                            Debug.Log($"  - Component: {comp.GetType().Name}");
                    }
                }
            }
        }
        
        void DebugMaterialsAndTextures()
        {
            Debug.Log("--- Materials and Textures Debug ---");
            
            var layeredVis = FindFirstObjectByType<LayeredTerrainVisualizer>();
            if (layeredVis != null && layeredVis.terrain != null)
            {
                var terrain = layeredVis.terrain;
                Debug.Log($"Terrain material template: {(terrain.materialTemplate != null ? terrain.materialTemplate.name : "NULL")}");
                
                if (terrain.materialTemplate != null)
                {
                    var mat = terrain.materialTemplate;
                    Debug.Log($"Material shader: {mat.shader.name}");
                    Debug.Log($"Main texture: {(mat.mainTexture != null ? $"{mat.mainTexture.name} ({mat.mainTexture.width}x{mat.mainTexture.height})" : "NULL")}");
                    
                    // Check if it's our generated texture
                    if (mat.mainTexture != null && mat.mainTexture.name.Contains("LayeredTerrain"))
                    {
                        Debug.Log("✓ Found LayeredTerrain texture - visualization should be working!");
                    }
                }
            }
        }
        
        [ContextMenu("Force Generate Visualization")]
        public void ForceGenerateVisualization()
        {
            Debug.Log("=== FORCING VISUALIZATION GENERATION ===");
            
            var layeredVis = FindFirstObjectByType<LayeredTerrainVisualizer>();
            if (layeredVis == null)
            {
                Debug.LogError("LayeredTerrainVisualizer not found!");
                return;
            }
            
            // Try to call the generation method directly
            try
            {
                layeredVis.SendMessage("GenerateLayeredVisualization", SendMessageOptions.DontRequireReceiver);
                Debug.Log("Called GenerateLayeredVisualization method");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error calling GenerateLayeredVisualization: {e.Message}");
            }
            
            // Wait a moment then debug again
            Invoke(nameof(DebugVisualizationSystem), 1f);
        }
        
        // DISABLED: Now managed by SimulationUIManager
        /*
        private void OnGUI()
        {
            if (!showDebugGUI) return;

            // Position on the right side of the screen
            float panelWidth = 400f;
            GUILayout.BeginArea(new Rect(Screen.width - panelWidth - 10, 10, panelWidth, Screen.height - 20));
            GUILayout.BeginVertical("box");

            GUILayout.Label("=== SIMULATION DEBUG ===", EditorStyles.boldLabel);
            
            if (GUILayout.Button("🔄 Refresh Debug Data"))
            {
                DebugVisualizationSystem();
            }
            
            if (GUILayout.Button("🎨 Generate Visualization"))
            {
                var visualizer = FindFirstObjectByType<LayeredTerrainVisualizer>();
                if (visualizer != null)
                {
                    visualizer.GenerateLayeredVisualization();
                }
            }

            GUILayout.Space(10);
            
            // Show real-time civilization data
            if (Application.isPlaying)
            {
                // Update cached data (UI now managed by SimulationUIManager)
                UpdateCachedData();
                
                ShowCivilizationStatus();
                GUILayout.Space(10);
                ShowRecentEvents();
                GUILayout.Space(10);
                ShowTerritories();
            }

            GUILayout.EndVertical();
            GUILayout.EndArea();
            
            // Time scale slider at bottom of screen
            float sliderWidth = 300f;
            float sliderHeight = 60f;
            GUILayout.BeginArea(new Rect((Screen.width - sliderWidth) / 2, Screen.height - sliderHeight - 10, sliderWidth, sliderHeight));
            GUILayout.BeginVertical("box");
            
            GUILayout.Label($"Simulation Speed: {_timeScale:F1}x", EditorStyles.boldLabel);
            float newTimeScale = GUILayout.HorizontalSlider(_timeScale, 0.1f, 10f);
            
            if (newTimeScale != _timeScale)
            {
                _timeScale = newTimeScale;
                Time.timeScale = _timeScale;
            }
            
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("0.5x")) { _timeScale = 0.5f; Time.timeScale = _timeScale; }
            if (GUILayout.Button("1x")) { _timeScale = 1f; Time.timeScale = _timeScale; }
            if (GUILayout.Button("2x")) { _timeScale = 2f; Time.timeScale = _timeScale; }
            if (GUILayout.Button("5x")) { _timeScale = 5f; Time.timeScale = _timeScale; }
            if (GUILayout.Button("10x")) { _timeScale = 10f; Time.timeScale = _timeScale; }
            GUILayout.EndHorizontal();
            
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
        */

        private void UpdateCachedData()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null) return;

            var entityManager = world.EntityManager;
            
            // Update cached civilizations
            _cachedCivilizations.Clear();
            var civQuery = entityManager.CreateEntityQuery(typeof(CivilizationData));
            var civilizations = civQuery.ToComponentDataArray<CivilizationData>(Unity.Collections.Allocator.Temp);
            
            for (int i = 0; i < civilizations.Length; i++)
            {
                if (civilizations[i].Population > 0) // Only cache active civilizations
                    _cachedCivilizations.Add(civilizations[i]);
            }
            civilizations.Dispose();
            
            // Update cached events from WorldHistorySystem
            _cachedEvents.Clear();
            if (_historySystem != null)
            {
                var events = _historySystem.GetHistoricalEvents(Unity.Collections.Allocator.Temp);
                
                for (int i = 0; i < events.Length; i++)
                {
                    _cachedEvents.Add(events[i]);
                }
                
                // Sort by year (most recent first)
                _cachedEvents.Sort((a, b) => b.Year.CompareTo(a.Year));
                
                events.Dispose();
                
                Debug.Log($"[VisualizationDebugger] Updated cached events: {_cachedEvents.Count} events from WorldHistorySystem");
            }
            
            // Update cached territories
            _cachedTerritories.Clear();
            var territoryQuery = entityManager.CreateEntityQuery(typeof(TerritoryData));
            if (territoryQuery.CalculateEntityCount() > 0)
            {
                var territories = territoryQuery.ToComponentDataArray<TerritoryData>(Unity.Collections.Allocator.Temp);
                
                for (int i = 0; i < territories.Length; i++)
                {
                    _cachedTerritories.Add(territories[i]);
                }
                
                territories.Dispose();
            }
        }

        private void ShowCivilizationStatus()
        {
            GUILayout.Label("=== CIVILIZATION STATUS ===", EditorStyles.boldLabel);
            
            if (_cachedCivilizations.Count == 0)
            {
                GUILayout.Label("No civilizations found");
                return;
            }

            GUILayout.Label($"Active Civilizations: {_cachedCivilizations.Count}");
            GUILayout.Space(5);

            // Scrollable area for civilizations
            _civilizationScrollPos = GUILayout.BeginScrollView(_civilizationScrollPos, GUILayout.Height(300f));
            
            for (int i = 0; i < _cachedCivilizations.Count; i++) // Show ALL civilizations
            {
                var civ = _cachedCivilizations[i];

                GUILayout.BeginVertical("box");
                GUILayout.Label($"🏛️ {civ.Name}", EditorStyles.boldLabel);
                GUILayout.Label($"👥 Population: {civ.Population:F0}");
                GUILayout.Label($"💰 Wealth: {civ.Wealth:F0}");
                GUILayout.Label($"🔬 Technology: {civ.Technology:F1}");
                GUILayout.Label($"⚖️ Stability: {(civ.Stability * 100):F0}%");
                GUILayout.Label($"🤝 Trade: {civ.Trade:F1}");
                GUILayout.Label($"⚔️ Military: {civ.Military:F1}");
                
                // Color-coded status
                if (civ.Stability < 0.3f)
                    GUILayout.Label("🔴 UNSTABLE", new GUIStyle(GUI.skin.label) { normal = { textColor = Color.red } });
                else if (civ.Population > 5000f)
                    GUILayout.Label("🟢 THRIVING", new GUIStyle(GUI.skin.label) { normal = { textColor = Color.green } });
                else
                    GUILayout.Label("🟡 STABLE", new GUIStyle(GUI.skin.label) { normal = { textColor = Color.yellow } });
                
                GUILayout.EndVertical();
                GUILayout.Space(3);
            }
            
            GUILayout.EndScrollView();
        }

        private void ShowRecentEvents()
        {
            GUILayout.Label("=== RECENT EVENTS ===", EditorStyles.boldLabel);
            
            if (_cachedEvents.Count == 0)
            {
                GUILayout.Label("No events recorded yet");
                if (_historySystem != null)
                {
                    GUILayout.Label($"History system found, but no events cached");
                }
                else
                {
                    GUILayout.Label("History system not found!");
                }
                return;
            }

            GUILayout.Label($"Total Events: {_cachedEvents.Count}");
            GUILayout.Space(5);

            // Scrollable area for events
            _eventsScrollPos = GUILayout.BeginScrollView(_eventsScrollPos, GUILayout.Height(250f));
            
            // Show most recent 20 events
            int eventsToShow = Mathf.Min(_cachedEvents.Count, 20);
            for (int i = 0; i < eventsToShow; i++)
            {
                var evt = _cachedEvents[i];
                
                GUILayout.BeginVertical("box");
                
                // Event icon based on category
                string icon = GetEventIconFromCategory(evt.Category.ToString());
                string colorHex = GetEventColorFromCategory(evt.Category.ToString());
                
                var style = new GUIStyle(GUI.skin.label);
                if (ColorUtility.TryParseHtmlString(colorHex, out Color color))
                    style.normal.textColor = color;
                
                GUILayout.Label($"{icon} {evt.Name}", style);
                GUILayout.Label($"Year {evt.Year} | Type: {evt.Type} | Significance: {evt.Significance:F1}", EditorStyles.miniLabel);
                
                // Wrap description text
                var descStyle = new GUIStyle(GUI.skin.label) { wordWrap = true, fontSize = 10 };
                GUILayout.Label(evt.Description.ToString(), descStyle);
                
                GUILayout.EndVertical();
                GUILayout.Space(2);
            }
            
            GUILayout.EndScrollView();
        }

        private void ShowTerritories()
        {
            GUILayout.Label("=== TERRITORIES & RUINS ===", EditorStyles.boldLabel);
            
            if (_cachedTerritories.Count == 0)
            {
                GUILayout.Label("No territories found");
                return;
            }

            GUILayout.Label($"Total Territories: {_cachedTerritories.Count}");
            
            // Count by type
            int cities = 0, monuments = 0, wonders = 0, ruins = 0;
            foreach (var territory in _cachedTerritories)
            {
                if (territory.IsRuined) ruins++;
                else if (territory.Type == TerritoryType.City) cities++;
                else if (territory.Type == TerritoryType.Wonder) wonders++;
                else monuments++;
            }
            
            GUILayout.Label($"🏙️ Cities: {cities} | 🏛️ Monuments: {monuments} | 🌟 Wonders: {wonders} | 💀 Ruins: {ruins}");
            GUILayout.Space(5);

            // Scrollable area for territories
            _territoriesScrollPos = GUILayout.BeginScrollView(_territoriesScrollPos, GUILayout.Height(200f));
            
            foreach (var territory in _cachedTerritories)
            {
                GUILayout.BeginVertical("box");
                
                string icon = territory.IsRuined ? "💀" :
                             territory.Type == TerritoryType.City ? "🏙️" :
                             territory.Type == TerritoryType.Wonder ? "🌟" :
                             territory.Type == TerritoryType.Temple ? "⛪" :
                             territory.Type == TerritoryType.Monument ? "🗿" :
                             territory.Type == TerritoryType.Academy ? "🎓" :
                             territory.Type == TerritoryType.Marketplace ? "🏪" :
                             territory.Type == TerritoryType.Palace ? "🏰" : "🏛️";
                
                var style = new GUIStyle(GUI.skin.label);
                if (territory.IsRuined)
                    style.normal.textColor = Color.gray;
                else
                    style.normal.textColor = Color.white;
                
                GUILayout.Label($"{icon} {territory.TerritoryName}", style);
                
                if (territory.IsRuined)
                {
                    GUILayout.Label($"💀 Ruined {territory.Type} (Originally: {territory.OriginalName})", EditorStyles.miniLabel);
                }
                else
                {
                    GUILayout.Label($"🏛️ {territory.Type} | 🛡️ Defense: {territory.DefenseStrength:F0}", EditorStyles.miniLabel);
                    if (territory.Population > 0)
                        GUILayout.Label($"👥 Population: {territory.Population:F0}", EditorStyles.miniLabel);
                }
                
                GUILayout.EndVertical();
                GUILayout.Space(2);
            }
            
            GUILayout.EndScrollView();
        }

        private string GetEventIconFromCategory(string category)
        {
            switch (category.ToLower())
            {
                case "war":
                case "military": return "⚔️";
                case "alliance":
                case "diplomacy": return "🤝";
                case "trade":
                case "economic": return "💰";
                case "political": return "🏛️";
                case "religious": return "⛪";
                case "social": return "👥";
                case "technological": return "🔬";
                case "cultural": return "🎭";
                case "natural": return "🌍";
                case "disaster": return "💥";
                case "discovery": return "🔍";
                case "expansion": return "🗺️";
                case "decline": return "📉";
                case "rise": return "📈";
                default: return "📰";
            }
        }

        private string GetEventColorFromCategory(string category)
        {
            switch (category.ToLower())
            {
                case "war":
                case "military": return "#FF4444"; // Red
                case "alliance":
                case "diplomacy": return "#44FF44"; // Green
                case "trade":
                case "economic": return "#FFFF44"; // Yellow
                case "political": return "#4444FF"; // Blue
                case "religious": return "#FF44FF"; // Magenta
                case "social": return "#FF8844"; // Orange
                case "technological": return "#44FFFF"; // Cyan
                case "cultural": return "#FF8844"; // Orange
                case "natural": return "#88FF44"; // Light Green
                case "disaster": return "#FF0000"; // Bright Red
                case "discovery": return "#00FFFF"; // Bright Cyan
                case "expansion": return "#00FF00"; // Bright Green
                case "decline": return "#FF6666"; // Light Red
                case "rise": return "#66FF66"; // Light Green
                default: return "#FFFFFF"; // White
            }
        }

        private string GetEventIcon(ProceduralWorld.Simulation.Core.EventType eventType)
        {
            switch (eventType)
            {
                case ProceduralWorld.Simulation.Core.EventType.Military: return "⚔️";
                case ProceduralWorld.Simulation.Core.EventType.Economic: return "💰";
                case ProceduralWorld.Simulation.Core.EventType.Political: return "🏛️";
                case ProceduralWorld.Simulation.Core.EventType.Religious: return "⛪";
                case ProceduralWorld.Simulation.Core.EventType.Social: return "👥";
                case ProceduralWorld.Simulation.Core.EventType.Technological: return "🔬";
                case ProceduralWorld.Simulation.Core.EventType.Cultural: return "🎭";
                case ProceduralWorld.Simulation.Core.EventType.Natural: return "🌍";
                default: return "📰";
            }
        }

        private string GetEventColor(ProceduralWorld.Simulation.Core.EventType eventType)
        {
            switch (eventType)
            {
                case ProceduralWorld.Simulation.Core.EventType.Military: return "#FF4444"; // Red
                case ProceduralWorld.Simulation.Core.EventType.Economic: return "#44FF44"; // Green
                case ProceduralWorld.Simulation.Core.EventType.Political: return "#4444FF"; // Blue
                case ProceduralWorld.Simulation.Core.EventType.Religious: return "#FF44FF"; // Magenta
                case ProceduralWorld.Simulation.Core.EventType.Social: return "#FFFF44"; // Yellow
                case ProceduralWorld.Simulation.Core.EventType.Technological: return "#44FFFF"; // Cyan
                case ProceduralWorld.Simulation.Core.EventType.Cultural: return "#FF8844"; // Orange
                case ProceduralWorld.Simulation.Core.EventType.Natural: return "#88FF44"; // Light Green
                default: return "#FFFFFF"; // White
            }
        }
    }
} 