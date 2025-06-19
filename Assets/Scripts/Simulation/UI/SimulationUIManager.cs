using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using ProceduralWorld.Simulation.Systems;
using ProceduralWorld.Simulation.Components;
using ProceduralWorld.Simulation.Core;
using ProceduralWorld.Simulation.Visualization;
using ProceduralWorld.Simulation.Camera;
using ProceduralWorld.Simulation.Utils;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ProceduralWorld.Simulation.UI
{
    public class SimulationUIManager : MonoBehaviour
    {
        [Header("UI Settings")]
        [SerializeField] private bool showMainUI = true;
        [SerializeField] private bool showDebugInfo = true;
        [SerializeField] private bool showVisualizationControls = true;
        [SerializeField] private bool showHistoryPanel = true;
        [SerializeField] private bool showCameraControls = true;
        
        // Panel visibility states
        private bool _mainPanelExpanded = true;
        private bool _debugPanelExpanded = true;
        private bool _visualizationPanelExpanded = true;
        private bool _historyPanelExpanded = false;
        private bool _cameraPanelExpanded = true;
        
        // UI Layout constants
        private const float PANEL_SPACING = 10f;
        private const float PANEL_WIDTH = 300f;
        private const float MAIN_PANEL_HEIGHT = 400f;
        private const float DEBUG_PANEL_HEIGHT = 500f;
        private const float VIS_PANEL_HEIGHT = 350f;
        private const float CAMERA_PANEL_HEIGHT = 250f;
        private const float HISTORY_PANEL_HEIGHT = 300f;
        private const float BOTTOM_PANEL_HEIGHT = 80f;
        
        // Scroll positions
        private Vector2 _mainScrollPos;
        private Vector2 _debugScrollPos;
        private Vector2 _historyScrollPos;
        
        // System references
        private WorldHistorySystem _historySystem;
        private LivingChronicleSystem _chronicleSystem;
        private ChronicleReaderUI _chronicleReaderUI;
        private ChronicleUIManager _chronicleUIManager;
        private LayeredTerrainVisualizer _visualizer;
        private SimulationCameraController _cameraController;
        private CameraFocusHelper _cameraFocusHelper;
        
        // Cached data
        private float _timeScale = 1f;
        private List<CivilizationData> _cachedCivs = new List<CivilizationData>();
        private List<HistoricalEventRecord> _cachedEvents = new List<HistoricalEventRecord>();
        private float _lastUIUpdate = 0f;
        private const float UI_UPDATE_INTERVAL = 0.5f;
        
        void Start()
        {
            // Initialize systems
            var world = World.DefaultGameObjectInjectionWorld;
            if (world?.EntityManager != null)
            {
                _historySystem = world.GetOrCreateSystemManaged<WorldHistorySystem>();
                _chronicleSystem = world.GetExistingSystemManaged<LivingChronicleSystem>();
                if (_chronicleSystem == null)
                {
                    Debug.LogWarning("[SimulationUIManager] LivingChronicleSystem not found - chronicle features will be disabled");
                }
            }
            
            // Find or create Chronicle UI Manager
            _chronicleUIManager = FindFirstObjectByType<ChronicleUIManager>();
            if (_chronicleUIManager == null)
            {
                // Create the Chronicle UI Manager
                var chronicleManagerGO = new GameObject("ChronicleUIManager");
                _chronicleUIManager = chronicleManagerGO.AddComponent<ChronicleUIManager>();
                Debug.Log("[SimulationUIManager] Created ChronicleUIManager");
            }
            
            // Get the chronicle reader from the manager
            _chronicleReaderUI = _chronicleUIManager.GetChronicleReader();
            
            // Find other components
            _visualizer = FindFirstObjectByType<LayeredTerrainVisualizer>();
            _cameraController = FindFirstObjectByType<SimulationCameraController>();
            _cameraFocusHelper = FindFirstObjectByType<CameraFocusHelper>();
            
            _timeScale = Time.timeScale;
            
            // Disable other GUI components to prevent overlap
            DisableOtherGUIComponents();
        }
        
        void DisableOtherGUIComponents()
        {
            // Disable OnGUI methods in other components but keep functionality
            var quickSetup = FindFirstObjectByType<QuickVisualizationSetup>();
            if (quickSetup != null) quickSetup.enabled = false;
            
            var debugger = FindFirstObjectByType<VisualizationDebugger>();
            if (debugger != null) debugger.enabled = false;
            
            // Camera controller should remain enabled for input handling
            // OnGUI is already commented out in these scripts
        }
        
        void Update()
        {
            // Update cached data periodically
            if (Time.time - _lastUIUpdate > UI_UPDATE_INTERVAL)
            {
                UpdateCachedData();
                _lastUIUpdate = Time.time;
            }
            
            // Handle keyboard shortcuts
            HandleInput();
        }
        
        void HandleInput()
        {
            if (Input.GetKeyDown(KeyCode.F1))
                showMainUI = !showMainUI;
            if (Input.GetKeyDown(KeyCode.F2))
                showDebugInfo = !showDebugInfo;
            if (Input.GetKeyDown(KeyCode.F3))
                showVisualizationControls = !showVisualizationControls;
            if (Input.GetKeyDown(KeyCode.F4))
                showHistoryPanel = !showHistoryPanel;
            if (Input.GetKeyDown(KeyCode.F5))
                showCameraControls = !showCameraControls;
            if (Input.GetKeyDown(KeyCode.F6) && _chronicleUIManager != null)
                _chronicleUIManager.OpenChronicleReader();
        }
        
        void UpdateCachedData()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world?.EntityManager == null) return;
            
            // Update civilizations
            var civQuery = world.EntityManager.CreateEntityQuery(typeof(CivilizationData));
            var civs = civQuery.ToComponentDataArray<CivilizationData>(Allocator.Temp);
            
            _cachedCivs.Clear();
            foreach (var civ in civs)
            {
                if (civ.IsActive)
                    _cachedCivs.Add(civ);
            }
            civs.Dispose();
            
            // Update events
            if (_historySystem != null)
            {
                var events = _historySystem.GetHistoricalEvents(Allocator.Temp);
                _cachedEvents.Clear();
                foreach (var evt in events)
                {
                    _cachedEvents.Add(evt);
                }
                events.Dispose();
            }
        }
        
        void OnGUI()
        {
            if (!Application.isPlaying || !showMainUI) return;
            
            GUI.skin.box.wordWrap = true;
            GUI.skin.label.wordWrap = true;
            
            DrawMainPanel();
            if (showVisualizationControls) DrawVisualizationPanel();
            if (showCameraControls) DrawCameraPanel();
            if (showDebugInfo) DrawDebugPanel();
            if (showHistoryPanel) DrawHistoryPanel();
            DrawTimeControlPanel();
            DrawHelpPanel();
        }
        
        void DrawMainPanel()
        {
            float x = PANEL_SPACING;
            float y = PANEL_SPACING;
            float width = PANEL_WIDTH;
            float height = _mainPanelExpanded ? MAIN_PANEL_HEIGHT : 30f;
            
            GUI.Box(new Rect(x, y, width, height), "");
            GUILayout.BeginArea(new Rect(x + 5, y + 5, width - 10, height - 10));
            
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(_mainPanelExpanded ? "▼" : "▶", GUILayout.Width(20)))
                _mainPanelExpanded = !_mainPanelExpanded;
            GUILayout.Label("SIMULATION STATUS", GetBoldLabelStyle());
            GUILayout.EndHorizontal();
            
            if (_mainPanelExpanded)
            {
                _mainScrollPos = GUILayout.BeginScrollView(_mainScrollPos);
                
                // Basic simulation info
                GUILayout.Space(5);
                GUILayout.Label($"🌍 Active Civilizations: {_cachedCivs.Count}");
                GUILayout.Label($"📚 Historical Events: {_cachedEvents.Count}");
                GUILayout.Label($"⏱️ Simulation Speed: {_timeScale:F1}x");
                
                GUILayout.Space(10);
                
                // Quick actions
                GUILayout.Label("=== QUICK ACTIONS ===", GetBoldLabelStyle());
                
                if (GUILayout.Button("📊 Spawn New Civilization"))
                {
                    Debug.Log("[SimulationUIManager] Spawn civilization requested");
                }
                
                if (GUILayout.Button("⚡ Generate Random Event"))
                {
                    Debug.Log("[SimulationUIManager] Random event generation requested");
                }
                
                if (GUILayout.Button("📜 Open Chronicle Reader") && _chronicleUIManager != null)
                {
                    _chronicleUIManager.OpenChronicleReader();
                }
                
                if (GUILayout.Button("🧪 Generate Test Event") && _historySystem != null)
                {
                    GenerateTestHistoricalEvent();
                }
                
                // Chronicle System Status
                GUILayout.Space(10);
                GUILayout.Label("=== CHRONICLE SYSTEM ===", GetBoldLabelStyle());
                
                if (_chronicleSystem != null && _chronicleUIManager != null)
                {
                    GUILayout.Label("📚 Living Chronicle System: ACTIVE", GetMiniLabelStyle());
                    GUILayout.Label("✨ Rich storytelling enabled", GetMiniLabelStyle());
                    
                    if (_chronicleUIManager.IsChronicleSystemReady())
                    {
                        GUILayout.Label("✅ Chronicle Reader: READY", GetMiniLabelStyle());
                        
                        if (GUILayout.Button("🔍 View Chronicles"))
                        {
                            _chronicleUIManager.OpenChronicleReader();
                        }
                        
                        if (GUILayout.Button("🧪 Test Chronicle UI"))
                        {
                            Debug.Log("[SimulationUIManager] Testing Chronicle UI - F6 should also work");
                            _chronicleUIManager.OpenChronicleReader();
                        }
                    }
                    else
                    {
                        GUILayout.Label("⏳ Chronicle Reader: INITIALIZING...", GetMiniLabelStyle());
                    }
                }
                else
                {
                    GUILayout.Label("❌ Chronicle System: NOT READY", GetMiniLabelStyle());
                    GUILayout.Label("⚠️ Using basic history only", GetMiniLabelStyle());
                    
                    if (_chronicleSystem == null)
                        GUILayout.Label("• Chronicle System: NULL", GetMiniLabelStyle());
                    if (_chronicleUIManager == null)
                        GUILayout.Label("• UI Manager: NULL", GetMiniLabelStyle());
                }
                
                // Civilization overview
                if (_cachedCivs.Count > 0)
                {
                    GUILayout.Label("=== CIVILIZATIONS ===", GetBoldLabelStyle());
                    
                    foreach (var civ in _cachedCivs)
                    {
                        GUILayout.BeginVertical("box");
                        
                        // Truncate long names
                        string displayName = civ.Name.ToString();
                        if (displayName.Length > 30)
                            displayName = displayName.Substring(0, 27) + "...";
                        
                        GUILayout.Label($"🏛️ {displayName}", GetBoldLabelStyle());
                        GUILayout.Label($"👥 Pop: {civ.Population:F0} | 💰 Wealth: {civ.Wealth:F0}");
                        GUILayout.Label($"🔬 Tech: {civ.Technology:F1} | ⚖️ Stability: {(civ.Stability * 100):F0}%");
                        
                        // Status indicator
                        if (civ.Stability < 0.3f)
                            GUILayout.Label("🔴 UNSTABLE", new GUIStyle(GUI.skin.label) { normal = { textColor = Color.red } });
                        else if (civ.Population > 5000f)
                            GUILayout.Label("🟢 THRIVING", new GUIStyle(GUI.skin.label) { normal = { textColor = Color.green } });
                        else
                            GUILayout.Label("🟡 STABLE", new GUIStyle(GUI.skin.label) { normal = { textColor = Color.yellow } });
                        
                        // Focus button
                        if (GUILayout.Button("🎯 Focus Camera"))
                        {
                            if (_cameraController != null)
                                _cameraController.FocusOnPosition(civ.Position, 50f);
                        }
                        
                        GUILayout.EndVertical();
                        GUILayout.Space(3);
                    }
                }
                
                GUILayout.EndScrollView();
            }
            
            GUILayout.EndArea();
        }
        
        void DrawVisualizationPanel()
        {
            if (_visualizer == null) return;
            
            float x = PANEL_SPACING;
            float y = PANEL_SPACING + (_mainPanelExpanded ? MAIN_PANEL_HEIGHT : 30f) + PANEL_SPACING;
            float width = PANEL_WIDTH;
            float height = _visualizationPanelExpanded ? VIS_PANEL_HEIGHT : 30f;
            
            GUI.Box(new Rect(x, y, width, height), "");
            GUILayout.BeginArea(new Rect(x + 5, y + 5, width - 10, height - 10));
            
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(_visualizationPanelExpanded ? "▼" : "▶", GUILayout.Width(20)))
                _visualizationPanelExpanded = !_visualizationPanelExpanded;
            GUILayout.Label("VISUALIZATION", GetBoldLabelStyle());
            GUILayout.EndHorizontal();
            
            if (_visualizationPanelExpanded)
            {
                GUILayout.Space(5);
                
                // Layer toggles
                _visualizer.showBiomes = GUILayout.Toggle(_visualizer.showBiomes, "🌿 Show Biomes");
                if (_visualizer.showBiomes)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(20);
                    GUILayout.Label("Opacity:");
                    _visualizer.biomeOpacity = GUILayout.HorizontalSlider(_visualizer.biomeOpacity, 0f, 1f);
                    GUILayout.EndHorizontal();
                }
                
                _visualizer.showCivilizations = GUILayout.Toggle(_visualizer.showCivilizations, "🏛️ Show Civilizations");
                if (_visualizer.showCivilizations)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(20);
                    GUILayout.Label("Opacity:");
                    _visualizer.civilizationOpacity = GUILayout.HorizontalSlider(_visualizer.civilizationOpacity, 0f, 1f);
                    GUILayout.EndHorizontal();
                }
                
                _visualizer.showReligions = GUILayout.Toggle(_visualizer.showReligions, "⛪ Show Religions");
                if (_visualizer.showReligions)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(20);
                    GUILayout.Label("Opacity:");
                    _visualizer.religionOpacity = GUILayout.HorizontalSlider(_visualizer.religionOpacity, 0f, 1f);
                    GUILayout.EndHorizontal();
                }
                
                _visualizer.showEconomies = GUILayout.Toggle(_visualizer.showEconomies, "💰 Show Economies");
                if (_visualizer.showEconomies)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(20);
                    GUILayout.Label("Opacity:");
                    _visualizer.economyOpacity = GUILayout.HorizontalSlider(_visualizer.economyOpacity, 0f, 1f);
                    GUILayout.EndHorizontal();
                }
                
                GUILayout.Space(10);
                
                // Refresh controls
                GUILayout.Label("=== REFRESH ===", GetBoldLabelStyle());
                _visualizer.enableAutoRefresh = GUILayout.Toggle(_visualizer.enableAutoRefresh, "🔄 Auto-Refresh");
                
                if (_visualizer.enableAutoRefresh)
                {
                    GUILayout.Label($"Interval: {_visualizer.autoRefreshInterval:F1}s");
                    _visualizer.autoRefreshInterval = GUILayout.HorizontalSlider(_visualizer.autoRefreshInterval, 0.5f, 10f);
                }
                
                if (GUILayout.Button("🎨 Manual Refresh"))
                {
                    _visualizer.GenerateLayeredVisualization();
                }
            }
            
            GUILayout.EndArea();
        }
        
        void DrawCameraPanel()
        {
            if (_cameraController == null) return;
            
            float x = PANEL_SPACING;
            float y = PANEL_SPACING + (_mainPanelExpanded ? MAIN_PANEL_HEIGHT : 30f) + PANEL_SPACING + 
                     (_visualizationPanelExpanded ? VIS_PANEL_HEIGHT : 30f) + PANEL_SPACING;
            float width = PANEL_WIDTH;
            float height = _cameraPanelExpanded ? CAMERA_PANEL_HEIGHT : 30f;
            
            GUI.Box(new Rect(x, y, width, height), "");
            GUILayout.BeginArea(new Rect(x + 5, y + 5, width - 10, height - 10));
            
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(_cameraPanelExpanded ? "▼" : "▶", GUILayout.Width(20)))
                _cameraPanelExpanded = !_cameraPanelExpanded;
            GUILayout.Label("CAMERA CONTROLS", GetBoldLabelStyle());
            GUILayout.EndHorizontal();
            
            if (_cameraPanelExpanded)
            {
                GUILayout.Space(5);
                
                // Quick focus buttons
                GUILayout.Label("=== QUICK FOCUS ===", GetBoldLabelStyle());
                
                if (GUILayout.Button("🌍 World Center"))
                {
                    _cameraController.FocusOnPosition(Vector3.zero, 100f);
                }
                
                if (GUILayout.Button("🏛️ Largest Civilization"))
                {
                    if (_cameraFocusHelper != null)
                        _cameraFocusHelper.FocusOnLargestCivilization();
                }
                
                if (GUILayout.Button("🔄 Cycle Civilizations"))
                {
                    if (_cameraFocusHelper != null)
                        _cameraFocusHelper.CycleToNextCivilization();
                }
                
                if (GUILayout.Button("📚 Random Event"))
                {
                    if (_cameraFocusHelper != null)
                        _cameraFocusHelper.FocusOnRandomRecentEvent();
                }
                
                GUILayout.Space(10);
                
                // Manual position control
                GUILayout.Label("=== MANUAL CONTROL ===", GetBoldLabelStyle());
                
                Vector3 currentPos = _cameraController.transform.position;
                GUILayout.Label($"Position: {currentPos:F1}");
                
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("⬆️")) _cameraController.SetPosition(currentPos + Vector3.forward * 50f);
                if (GUILayout.Button("⬇️")) _cameraController.SetPosition(currentPos + Vector3.back * 50f);
                if (GUILayout.Button("⬅️")) _cameraController.SetPosition(currentPos + Vector3.left * 50f);
                if (GUILayout.Button("➡️")) _cameraController.SetPosition(currentPos + Vector3.right * 50f);
                GUILayout.EndHorizontal();
                
                GUILayout.Space(5);
                
                // Zoom control
                var cam = _cameraController.GetComponent<UnityEngine.Camera>();
                if (cam != null)
                {
                    GUILayout.Label("=== ZOOM ===", GetBoldLabelStyle());
                    float currentZoom = cam.orthographic ? cam.orthographicSize : cam.fieldOfView;
                    GUILayout.Label($"Zoom: {currentZoom:F1}");
                    
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("🔍 Zoom In")) _cameraController.SetZoom(currentZoom * 0.8f);
                    if (GUILayout.Button("🔍 Zoom Out")) _cameraController.SetZoom(currentZoom * 1.2f);
                    GUILayout.EndHorizontal();
                }
                
                GUILayout.Space(5);
                
                // Keyboard shortcuts info
                GUILayout.Label("=== HOTKEYS ===", GetBoldLabelStyle());
                GUILayout.Label("WASD: Move camera", GetMiniLabelStyle());
                GUILayout.Label("Mouse Wheel: Zoom", GetMiniLabelStyle());
                GUILayout.Label("Middle Mouse: Drag", GetMiniLabelStyle());
                GUILayout.Label("Q: Focus largest civ", GetMiniLabelStyle());
                GUILayout.Label("E: Random event", GetMiniLabelStyle());
                GUILayout.Label("C: World center", GetMiniLabelStyle());
                GUILayout.Label("Tab: Cycle civs", GetMiniLabelStyle());
            }
            
            GUILayout.EndArea();
        }
        
        void DrawDebugPanel()
        {
            float x = Screen.width - PANEL_WIDTH - PANEL_SPACING;
            float y = PANEL_SPACING;
            float width = PANEL_WIDTH;
            float height = _debugPanelExpanded ? DEBUG_PANEL_HEIGHT : 30f;
            
            GUI.Box(new Rect(x, y, width, height), "");
            GUILayout.BeginArea(new Rect(x + 5, y + 5, width - 10, height - 10));
            
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(_debugPanelExpanded ? "▼" : "▶", GUILayout.Width(20)))
                _debugPanelExpanded = !_debugPanelExpanded;
            GUILayout.Label("DEBUG INFO", GetBoldLabelStyle());
            GUILayout.EndHorizontal();
            
            if (_debugPanelExpanded)
            {
                _debugScrollPos = GUILayout.BeginScrollView(_debugScrollPos);
                
                // Camera info
                if (_cameraController != null)
                {
                    GUILayout.Label("=== CAMERA ===", GetBoldLabelStyle());
                    GUILayout.Label($"Position: {_cameraController.transform.position:F1}");
                    var cam = _cameraController.GetComponent<UnityEngine.Camera>();
                    if (cam != null)
                    {
                        GUILayout.Label($"Zoom: {(cam.orthographic ? cam.orthographicSize : cam.fieldOfView):F1}");
                    }
                    GUILayout.Space(5);
                }
                
                // System info
                GUILayout.Label("=== PERFORMANCE ===", GetBoldLabelStyle());
                GUILayout.Label($"FPS: {(1f / Time.unscaledDeltaTime):F0}");
                GUILayout.Label($"Frame Time: {Time.unscaledDeltaTime * 1000:F1}ms");
                GUILayout.Label($"Time Scale: {Time.timeScale:F1}x");
                GUILayout.Space(5);
                
                // Entity counts
                var world = World.DefaultGameObjectInjectionWorld;
                if (world?.EntityManager != null)
                {
                    GUILayout.Label("=== ENTITIES ===", GetBoldLabelStyle());
                    
                    var civQuery = world.EntityManager.CreateEntityQuery(typeof(CivilizationData));
                    var relQuery = world.EntityManager.CreateEntityQuery(typeof(ReligionData));
                    var terrQuery = world.EntityManager.CreateEntityQuery(typeof(TerritoryData));
                    
                    GUILayout.Label($"Civilizations: {civQuery.CalculateEntityCount()}");
                    GUILayout.Label($"Religions: {relQuery.CalculateEntityCount()}");
                    GUILayout.Label($"Territories: {terrQuery.CalculateEntityCount()}");
                }
                
                GUILayout.EndScrollView();
            }
            
            GUILayout.EndArea();
        }
        
        void DrawHistoryPanel()
        {
            float x = Screen.width - PANEL_WIDTH - PANEL_SPACING;
            float y = PANEL_SPACING + (_debugPanelExpanded ? DEBUG_PANEL_HEIGHT : 30f) + PANEL_SPACING;
            float width = PANEL_WIDTH;
            float height = _historyPanelExpanded ? HISTORY_PANEL_HEIGHT : 30f;
            
            GUI.Box(new Rect(x, y, width, height), "");
            GUILayout.BeginArea(new Rect(x + 5, y + 5, width - 10, height - 10));
            
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(_historyPanelExpanded ? "▼" : "▶", GUILayout.Width(20)))
                _historyPanelExpanded = !_historyPanelExpanded;
            GUILayout.Label("RECENT EVENTS", GetBoldLabelStyle());
            GUILayout.EndHorizontal();
            
            if (_historyPanelExpanded)
            {
                _historyScrollPos = GUILayout.BeginScrollView(_historyScrollPos);
                
                if (_cachedEvents.Count > 0)
                {
                    // Sort by year and show recent events
                    var sortedEvents = new List<HistoricalEventRecord>(_cachedEvents);
                    sortedEvents.Sort((a, b) => b.Year.CompareTo(a.Year));
                    
                    int eventsToShow = Mathf.Min(10, sortedEvents.Count);
                    for (int i = 0; i < eventsToShow; i++)
                    {
                        var evt = sortedEvents[i];
                        
                        GUILayout.BeginVertical("box");
                        string icon = GetEventIcon(evt.Category.ToString());
                        GUILayout.Label($"{icon} {evt.Name}", GetBoldLabelStyle());
                        GUILayout.Label($"Year {evt.Year} | {evt.Type}", GetMiniLabelStyle());
                        
                        // Wrap long descriptions
                        string desc = evt.Description.ToString();
                        if (desc.Length > 60)
                            desc = desc.Substring(0, 57) + "...";
                        GUILayout.Label(desc, new GUIStyle(GUI.skin.label) { wordWrap = true });
                        
                        GUILayout.EndVertical();
                        GUILayout.Space(2);
                    }
                }
                else
                {
                    GUILayout.Label("No events recorded yet.");
                    GUILayout.Label("Let the simulation run!");
                }
                
                GUILayout.EndScrollView();
            }
            
            GUILayout.EndArea();
        }
        
        void DrawTimeControlPanel()
        {
            float width = 400f;
            float height = BOTTOM_PANEL_HEIGHT;
            float x = (Screen.width - width) / 2;
            float y = Screen.height - height - PANEL_SPACING;
            
            GUI.Box(new Rect(x, y, width, height), "");
            GUILayout.BeginArea(new Rect(x + 5, y + 5, width - 10, height - 10));
            
            GUILayout.Label($"⏱️ Simulation Speed: {_timeScale:F1}x", GetBoldLabelStyle());
            
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
            
            GUILayout.EndArea();
        }
        
        void DrawHelpPanel()
        {
            float width = 200f;
            float height = 120f;
            float x = Screen.width - width - PANEL_SPACING;
            float y = Screen.height - height - PANEL_SPACING;
            
            GUI.Box(new Rect(x, y, width, height), "");
            GUILayout.BeginArea(new Rect(x + 5, y + 5, width - 10, height - 10));
            
            GUILayout.Label("CONTROLS", GetBoldLabelStyle());
            GUILayout.Label("F1: Toggle Main UI", GetMiniLabelStyle());
            GUILayout.Label("F2: Toggle Debug", GetMiniLabelStyle());
            GUILayout.Label("F3: Toggle Visualization", GetMiniLabelStyle());
            GUILayout.Label("F4: Toggle History", GetMiniLabelStyle());
            GUILayout.Label("F5: Toggle Camera", GetMiniLabelStyle());
            GUILayout.Label("F6: Toggle Chronicle Reader", GetMiniLabelStyle());
            GUILayout.Label("WASD: Move Camera", GetMiniLabelStyle());
            GUILayout.Label("Mouse Wheel: Zoom", GetMiniLabelStyle());
            
            GUILayout.EndArea();
        }
        
        string GetEventIcon(string category)
        {
            return category.ToLower() switch
            {
                "disaster" => "🌋",
                "golden" => "⭐",
                "military" => "⚔️",
                "conflict" => "💥",
                "coalition" => "🤝",
                "holywar" => "⛪",
                "betrayal" => "🗡️",
                "hero" => "🦸",
                "revolution" => "🔥",
                "spiritual" => "✨",
                "discovery" => "🔍",
                _ => "📜"
            };
        }
        
        // Helper methods for cross-platform GUI styles
        GUIStyle GetBoldLabelStyle()
        {
#if UNITY_EDITOR
            return EditorStyles.boldLabel;
#else
            return new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
#endif
        }
        
        GUIStyle GetMiniLabelStyle()
        {
#if UNITY_EDITOR
            return EditorStyles.miniLabel;
#else
            return new GUIStyle(GUI.skin.label) { fontSize = 10 };
#endif
        }
        
        // ==== UI UTILITY METHODS ====
        
        private void GenerateTestHistoricalEvent()
        {
            Debug.Log("[SimulationUIManager] Generating test historical event...");
            
            // Find a civilization to use for the test
            var world = World.DefaultGameObjectInjectionWorld;
            if (world?.EntityManager == null)
            {
                Debug.LogWarning("[SimulationUIManager] Cannot generate test event - no ECS world");
                return;
            }
            
            var entityManager = world.EntityManager;
            var civQuery = entityManager.CreateEntityQuery(typeof(CivilizationData));
            var civEntities = civQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
            
            if (civEntities.Length == 0)
            {
                Debug.LogWarning("[SimulationUIManager] Cannot generate test event - no civilizations found");
                civEntities.Dispose();
                civQuery.Dispose();
                return;
            }
            
            var testCivEntity = civEntities[0];
            var civData = entityManager.GetComponentData<CivilizationData>(testCivEntity);
            
            var testEvent = new HistoricalEventRecord
            {
                Title = new FixedString128Bytes("Test Chronicle Event"),
                Description = new FixedString512Bytes($"This is a test event generated to verify the chronicle system is working for {civData.Name}."),
                Year = (int)Time.time,
                Type = ProceduralWorld.Simulation.Core.EventType.Cultural,
                Category = EventCategory.Cultural,
                Location = civData.Position,
                Significance = 2.0f,
                SourceEntityId = Entity.Null,
                Size = 1.0f,
                CivilizationId = testCivEntity
            };
            
            _historySystem.AddEvent(testEvent);
            
            Debug.Log($"[SimulationUIManager] Generated test event for {civData.Name}: {testEvent.Title}");
            
            civEntities.Dispose();
            civQuery.Dispose();
        }
    }
} 