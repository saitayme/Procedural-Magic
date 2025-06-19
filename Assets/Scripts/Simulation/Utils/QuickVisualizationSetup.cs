using UnityEngine;
using ProceduralWorld.Simulation.Visualization;

namespace ProceduralWorld.Simulation.Utils
{
    public class QuickVisualizationSetup : MonoBehaviour
    {
        [Header("Quick Setup")]
        [SerializeField] private bool setupOnStart = true;
        
        void Start()
        {
            if (setupOnStart)
            {
                SetupVisualization();
            }
        }
        
        [ContextMenu("Setup Visualization Now")]
        public void SetupVisualization()
        {
            Debug.Log("[QuickVisualizationSetup] Setting up visualization...");
            
            // Find the LayeredTerrainVisualizer
            var visualizer = FindFirstObjectByType<LayeredTerrainVisualizer>();
            if (visualizer == null)
            {
                Debug.LogError("[QuickVisualizationSetup] LayeredTerrainVisualizer not found in scene!");
                return;
            }
            
            // Auto-assign terrain if not set (skip for custom mesh-based terrain systems)
            if (visualizer.terrain == null)
            {
                var terrain = FindFirstObjectByType<Terrain>();
                if (terrain != null)
                {
                    visualizer.terrain = terrain;
                    Debug.Log("[QuickVisualizationSetup] Auto-assigned terrain to LayeredTerrainVisualizer");
                }
                // Note: No warning for custom mesh-based terrain systems
            }
            
            // Enable biome visualization by default
            visualizer.showBiomes = true;
            visualizer.showCivilizations = false;
            visualizer.showReligions = false;
            visualizer.showEconomies = false;
            
            Debug.Log("[QuickVisualizationSetup] Visualization setup complete!");
            Debug.Log("[QuickVisualizationSetup] Controls:");
            Debug.Log("- Use the GUI controls in the top-left corner during play");
            Debug.Log("- Auto-refresh is enabled by default (every 2 seconds)");
            Debug.Log("- Press R to manually refresh the visualization (if manual refresh is enabled)");
            Debug.Log("- Toggle different layers on/off to see different aspects of your world");
        }
        
        // DISABLED: Now managed by SimulationUIManager
        /*
        void OnGUI()
        {
            if (!Application.isPlaying) return;
            
            GUILayout.BeginArea(new Rect(10, 200, 300, 100));
            GUILayout.Label("Quick Setup Controls:");
            
            if (GUILayout.Button("Setup Visualization"))
            {
                SetupVisualization();
            }
            
            if (GUILayout.Button("Force Refresh All"))
            {
                var visualizer = FindFirstObjectByType<LayeredTerrainVisualizer>();
                if (visualizer != null)
                {
                    visualizer.SendMessage("GenerateLayeredVisualization", SendMessageOptions.DontRequireReceiver);
                    Debug.Log("[QuickVisualizationSetup] Forced refresh of LayeredTerrainVisualizer");
                }
            }
            
            GUILayout.EndArea();
        }
        */
    }
} 