using UnityEngine;
using ProceduralWorld.Simulation.UI;
using ProceduralWorld.Simulation.Camera;

namespace ProceduralWorld.Simulation.Utils
{
    /// <summary>
    /// Helper script to automatically set up the SimulationUIManager in the scene
    /// Add this to any GameObject in the scene to ensure clean UI management
    /// </summary>
    public class UISetupHelper : MonoBehaviour
    {
        [Header("Auto-Setup Settings")]
        [SerializeField] private bool autoSetupOnStart = true;
        [SerializeField] private bool destroyAfterSetup = true;
        
        void Start()
        {
            if (autoSetupOnStart)
            {
                SetupUIManager();
                
                if (destroyAfterSetup)
                {
                    Destroy(this);
                }
            }
        }
        
        [ContextMenu("Setup UI Manager")]
        public void SetupUIManager()
        {
            // Check if SimulationUIManager already exists
            var existingUIManager = FindFirstObjectByType<SimulationUIManager>();
            if (existingUIManager != null)
            {
                Debug.Log("[UISetupHelper] SimulationUIManager already exists in scene");
                return;
            }
            
            // Create new GameObject for UI Manager
            var uiManagerGO = new GameObject("SimulationUIManager");
            var uiManager = uiManagerGO.AddComponent<SimulationUIManager>();
            
            // Position it appropriately (doesn't matter for UI, but good practice)
            uiManagerGO.transform.position = Vector3.zero;
            
            Debug.Log("[UISetupHelper] Created SimulationUIManager in scene - UI should now be properly organized!");
            
            // Disable overlapping UI components
            DisableOverlappingUI();
        }
        
        void DisableOverlappingUI()
        {
            Debug.Log("[UISetupHelper] Disabling overlapping UI components...");
            
            // Disable other UI components that might still be active
            var quickSetup = FindFirstObjectByType<QuickVisualizationSetup>();
            if (quickSetup != null) 
            {
                quickSetup.enabled = false;
                Debug.Log("[UISetupHelper] Disabled QuickVisualizationSetup");
            }
            
            var debugger = FindFirstObjectByType<VisualizationDebugger>();
            if (debugger != null) 
            {
                debugger.enabled = false;
                Debug.Log("[UISetupHelper] Disabled VisualizationDebugger");
            }
            
            var cameraHelper = FindFirstObjectByType<CameraFocusHelper>();
            if (cameraHelper != null) 
            {
                // Don't disable the component, just the GUI
                Debug.Log("[UISetupHelper] CameraFocusHelper OnGUI disabled via code comments");
            }
            
            var worldHistory = FindFirstObjectByType<WorldHistoryUI>();
            if (worldHistory != null) 
            {
                // Don't disable the component, just the GUI
                Debug.Log("[UISetupHelper] WorldHistoryUI OnGUI disabled via code comments");
            }
            
            var worldNameDisplay = FindFirstObjectByType<WorldNameDisplay>();
            if (worldNameDisplay != null) 
            {
                // Don't disable the component, just the GUI
                Debug.Log("[UISetupHelper] WorldNameDisplay OnGUI disabled via code comments");
            }
        }
    }
} 