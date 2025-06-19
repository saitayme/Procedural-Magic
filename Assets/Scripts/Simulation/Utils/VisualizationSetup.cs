using UnityEngine;
using ProceduralWorld.Simulation.Visualization;

namespace ProceduralWorld.Simulation.Utils
{
    public class VisualizationSetup : MonoBehaviour
    {
        [Header("Auto Setup")]
        public bool autoSetupOnStart = true;
        public bool replaceOldVisualizers = true;
        
        [Header("Prefab References")]
        public GameObject layeredTerrainVisualizerPrefab;
        
        void Start()
        {
            if (autoSetupOnStart)
            {
                SetupVisualization();
            }
        }
        
        [ContextMenu("Setup Visualization System")]
        public void SetupVisualization()
        {
            Debug.Log("[VisualizationSetup] Setting up visualization system...");
            
            // Remove old visualizers if requested
            if (replaceOldVisualizers)
            {
                RemoveOldVisualizers();
            }
            
            // Add LayeredTerrainVisualizer if not present
            SetupLayeredTerrainVisualizer();
            
            Debug.Log("[VisualizationSetup] Visualization system setup complete!");
        }
        
        void RemoveOldVisualizers()
        {
            // Remove old TerrainColorVisualizer components (if any exist)
            // Note: TerrainColorVisualizer is deprecated, use LayeredTerrainVisualizer instead
            Debug.Log("[VisualizationSetup] Checking for old visualizers to remove...");
        }
        
        void SetupLayeredTerrainVisualizer()
        {
            // Check if LayeredTerrainVisualizer already exists
            var existingVisualizer = FindFirstObjectByType<LayeredTerrainVisualizer>();
            if (existingVisualizer != null)
            {
                Debug.Log("[VisualizationSetup] LayeredTerrainVisualizer already exists");
                return;
            }
            
            GameObject visualizerObj = null;
            
            // Try to instantiate from prefab first
            if (layeredTerrainVisualizerPrefab != null)
            {
                visualizerObj = Instantiate(layeredTerrainVisualizerPrefab);
                Debug.Log("[VisualizationSetup] Created LayeredTerrainVisualizer from prefab");
            }
            else
            {
                // Create manually if no prefab
                visualizerObj = new GameObject("LayeredTerrainVisualizer");
                var visualizer = visualizerObj.AddComponent<LayeredTerrainVisualizer>();
                
                // Auto-find terrain
                var terrain = FindFirstObjectByType<Terrain>();
                if (terrain != null)
                {
                    visualizer.terrain = terrain;
                    Debug.Log("[VisualizationSetup] Auto-assigned terrain to LayeredTerrainVisualizer");
                }
                
                Debug.Log("[VisualizationSetup] Created LayeredTerrainVisualizer manually");
            }
            
            // Position it appropriately
            if (visualizerObj != null)
            {
                visualizerObj.transform.position = Vector3.zero;
            }
        }
        
        [ContextMenu("Force Refresh All Visualizations")]
        public void ForceRefreshVisualizations()
        {
            // Refresh LayeredTerrainVisualizer
            var layeredVisualizer = FindFirstObjectByType<LayeredTerrainVisualizer>();
            if (layeredVisualizer != null)
            {
                // Trigger refresh by simulating R key press
                layeredVisualizer.SendMessage("GenerateLayeredVisualization", SendMessageOptions.DontRequireReceiver);
                Debug.Log("[VisualizationSetup] Refreshed LayeredTerrainVisualizer");
            }
            
            // Refresh other visualizers
            var biomeVisualizers = FindObjectsByType<BiomeLabelVisualizer>(FindObjectsSortMode.None);
            foreach (var visualizer in biomeVisualizers)
            {
                visualizer.SendMessage("Update", SendMessageOptions.DontRequireReceiver);
            }
            
            var entityVisualizers = FindObjectsByType<EntityMarkerVisualizer>(FindObjectsSortMode.None);
            foreach (var visualizer in entityVisualizers)
            {
                visualizer.SendMessage("Update", SendMessageOptions.DontRequireReceiver);
            }
            
            Debug.Log("[VisualizationSetup] Refreshed all visualizations");
        }
    }
} 