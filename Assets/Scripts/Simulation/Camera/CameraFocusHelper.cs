using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using ProceduralWorld.Simulation.Components;
using ProceduralWorld.Simulation.Systems;

namespace ProceduralWorld.Simulation.Camera
{
    public class CameraFocusHelper : MonoBehaviour
    {
        [Header("Focus Settings")]
        [SerializeField] private float focusZoom = 40f;
        [SerializeField] private float eventFocusZoom = 50f;
        [SerializeField] private float civilizationFocusZoom = 35f;
        
        [Header("Hotkeys")]
        [SerializeField] private KeyCode focusOnLargestCivKey = KeyCode.F;
        [SerializeField] private KeyCode focusOnRandomEventKey = KeyCode.E;
        [SerializeField] private KeyCode focusOnCenterKey = KeyCode.C;
        [SerializeField] private KeyCode cycleNextCivKey = KeyCode.Tab;
        
        private SimulationCameraController _cameraController;
        private EntityManager _entityManager;
        private WorldHistorySystem _historySystem;
        private int _currentCivIndex = 0;
        
        void Start()
        {
            _cameraController = GetComponent<SimulationCameraController>();
            if (_cameraController == null)
            {
                Debug.LogError("[CameraFocusHelper] No SimulationCameraController found!");
                enabled = false;
                return;
            }
            
            var world = World.DefaultGameObjectInjectionWorld;
            if (world != null)
            {
                _entityManager = world.EntityManager;
                _historySystem = world.GetOrCreateSystemManaged<WorldHistorySystem>();
            }
            
            Debug.Log("[CameraFocusHelper] Camera focus helper initialized");
        }
        
        void Update()
        {
            HandleHotkeys();
        }
        
        void HandleHotkeys()
        {
            if (Input.GetKeyDown(focusOnLargestCivKey))
            {
                FocusOnLargestCivilization();
            }
            
            if (Input.GetKeyDown(focusOnRandomEventKey))
            {
                FocusOnRandomRecentEvent();
            }
            
            if (Input.GetKeyDown(focusOnCenterKey))
            {
                FocusOnWorldCenter();
            }
            
            if (Input.GetKeyDown(cycleNextCivKey))
            {
                CycleToNextCivilization();
            }
        }
        
        public void FocusOnLargestCivilization()
        {
            if (_entityManager == null) return;
            
            var civilizationQuery = _entityManager.CreateEntityQuery(typeof(CivilizationData));
            if (civilizationQuery.CalculateEntityCount() == 0)
            {
                Debug.Log("[CameraFocusHelper] No civilizations found to focus on");
                return;
            }
            
            var civilizations = civilizationQuery.ToEntityArray(Allocator.Temp);
            var civDataList = civilizationQuery.ToComponentDataArray<CivilizationData>(Allocator.Temp);
            
            if (civDataList.Length == 0)
            {
                Debug.Log("[CameraFocusHelper] No civilization data found");
                civilizations.Dispose();
                civDataList.Dispose();
                return;
            }
            
            // Find largest civilization by population
            int largestIndex = 0;
            float largestPopulation = 0f;
            
            for (int i = 0; i < civDataList.Length; i++)
            {
                if (civDataList[i].Population > largestPopulation && civDataList[i].IsActive)
                {
                    largestPopulation = civDataList[i].Population;
                    largestIndex = i;
                }
            }
            
            if (largestPopulation > 0f)
            {
                var targetCiv = civDataList[largestIndex];
                _cameraController.FocusOnPosition(targetCiv.Position, civilizationFocusZoom);
                Debug.Log($"[CameraFocusHelper] Focused on largest civilization: {targetCiv.Name} (Population: {targetCiv.Population:F0})");
            }
            
            civilizations.Dispose();
            civDataList.Dispose();
        }
        
        public void CycleToNextCivilization()
        {
            if (_entityManager == null) return;
            
            var civilizationQuery = _entityManager.CreateEntityQuery(typeof(CivilizationData));
            var civDataList = civilizationQuery.ToComponentDataArray<CivilizationData>(Allocator.Temp);
            
            if (civDataList.Length == 0)
            {
                Debug.Log("[CameraFocusHelper] No civilizations found to cycle through");
                civDataList.Dispose();
                return;
            }
            
            // Find next active civilization
            int startIndex = _currentCivIndex;
            do
            {
                _currentCivIndex = (_currentCivIndex + 1) % civDataList.Length;
                
                if (civDataList[_currentCivIndex].IsActive)
                {
                    var targetCiv = civDataList[_currentCivIndex];
                    _cameraController.FocusOnPosition(targetCiv.Position, civilizationFocusZoom);
                    Debug.Log($"[CameraFocusHelper] Cycled to civilization: {targetCiv.Name} (Population: {targetCiv.Population:F0})");
                    break;
                }
            }
            while (_currentCivIndex != startIndex);
            
            civDataList.Dispose();
        }
        
        public void FocusOnRandomRecentEvent()
        {
            if (_historySystem == null) return;
            
            var events = _historySystem.GetHistoricalEvents(Allocator.Temp);
            if (events.Length == 0)
            {
                Debug.Log("[CameraFocusHelper] No historical events found to focus on");
                events.Dispose();
                return;
            }
            
            // Get recent events (last 10 or all if less than 10)
            int recentCount = Mathf.Min(10, events.Length);
            int randomIndex = UnityEngine.Random.Range(Mathf.Max(0, events.Length - recentCount), events.Length);
            
            var targetEvent = events[randomIndex];
            _cameraController.FocusOnPosition(targetEvent.Location, eventFocusZoom);
            Debug.Log($"[CameraFocusHelper] Focused on event: {targetEvent.Title} (Year: {targetEvent.Year})");
            
            events.Dispose();
        }
        
        public void FocusOnWorldCenter()
        {
            _cameraController.FocusOnPosition(Vector3.zero, focusZoom);
            Debug.Log("[CameraFocusHelper] Focused on world center");
        }
        
        public void FocusOnPosition(Vector3 position, float zoom = -1f)
        {
            float targetZoom = zoom > 0f ? zoom : focusZoom;
            _cameraController.FocusOnPosition(position, targetZoom);
            Debug.Log($"[CameraFocusHelper] Focused on position: {position:F1} with zoom: {targetZoom:F1}");
        }
        
        public void FocusOnCivilization(string civilizationName)
        {
            if (_entityManager == null) return;
            
            var civilizationQuery = _entityManager.CreateEntityQuery(typeof(CivilizationData));
            var civDataList = civilizationQuery.ToComponentDataArray<CivilizationData>(Allocator.Temp);
            
            for (int i = 0; i < civDataList.Length; i++)
            {
                if (civDataList[i].Name.ToString().Contains(civilizationName) && civDataList[i].IsActive)
                {
                    var targetCiv = civDataList[i];
                    _cameraController.FocusOnPosition(targetCiv.Position, civilizationFocusZoom);
                    Debug.Log($"[CameraFocusHelper] Focused on civilization: {targetCiv.Name}");
                    _currentCivIndex = i;
                    break;
                }
            }
            
            civDataList.Dispose();
        }
        
        // GUI for additional controls - DISABLED: Now managed by SimulationUIManager
        /*
        void OnGUI()
        {
            if (!Application.isPlaying) return;
            
            GUILayout.BeginArea(new Rect(Screen.width - 250, 10, 240, 300));
#if UNITY_EDITOR
            GUILayout.Label("Camera Focus Controls:", UnityEditor.EditorGUIUtility.isProSkin ? GUI.skin.box : GUI.skin.label);
#else
            GUILayout.Label("Camera Focus Controls:", GUI.skin.label);
#endif
            
            if (GUILayout.Button($"Focus on Largest Civ ({focusOnLargestCivKey})"))
            {
                FocusOnLargestCivilization();
            }
            
            if (GUILayout.Button($"Cycle Civilizations ({cycleNextCivKey})"))
            {
                CycleToNextCivilization();
            }
            
            if (GUILayout.Button($"Random Recent Event ({focusOnRandomEventKey})"))
            {
                FocusOnRandomRecentEvent();
            }
            
            if (GUILayout.Button($"World Center ({focusOnCenterKey})"))
            {
                FocusOnWorldCenter();
            }
            
            GUILayout.Space(10);
            
            // Quick civilization list
            if (_entityManager != null)
            {
                var civilizationQuery = _entityManager.CreateEntityQuery(typeof(CivilizationData));
                var civDataList = civilizationQuery.ToComponentDataArray<CivilizationData>(Allocator.Temp);
                
                if (civDataList.Length > 0)
                {
                    GUILayout.Label("Quick Focus:");
                    
                    // Show top 5 civilizations by population
                    var sortedCivs = new System.Collections.Generic.List<(CivilizationData civ, int index)>();
                    for (int i = 0; i < civDataList.Length; i++)
                    {
                        if (civDataList[i].IsActive)
                        {
                            sortedCivs.Add((civDataList[i], i));
                        }
                    }
                    
                    sortedCivs.Sort((a, b) => b.civ.Population.CompareTo(a.civ.Population));
                    
                    int displayCount = Mathf.Min(5, sortedCivs.Count);
                    for (int i = 0; i < displayCount; i++)
                    {
                        var civ = sortedCivs[i].civ;
                        string buttonText = $"{civ.Name} ({civ.Population:F0})";
                        if (buttonText.Length > 25)
                        {
                            buttonText = buttonText.Substring(0, 22) + "...";
                        }
                        
                        if (GUILayout.Button(buttonText))
                        {
                            _cameraController.FocusOnPosition(civ.Position, civilizationFocusZoom);
                            _currentCivIndex = sortedCivs[i].index;
                        }
                    }
                }
                
                civDataList.Dispose();
            }
            
            GUILayout.EndArea();
        }
        */
    }
} 