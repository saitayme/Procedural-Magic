using UnityEngine;
using Unity.Entities;

namespace ProceduralWorld.Simulation.UI
{
    /// <summary>
    /// Manages the Chronicle UI system - automatically creates and initializes the Chronicle Reader
    /// </summary>
    public class ChronicleUIManager : MonoBehaviour
    {
        [Header("Auto Setup")]
        [SerializeField] private bool autoCreateChronicleReader = true;
        [SerializeField] private bool showStartupMessage = true;
        
        private ChronicleReaderUI _chronicleReader;
        private bool _isInitialized = false;
        
        void Start()
        {
            if (autoCreateChronicleReader)
            {
                SetupChronicleReader();
            }
        }
        
        void Update()
        {
            // Check if systems are ready
            if (!_isInitialized)
            {
                var world = World.DefaultGameObjectInjectionWorld;
                if (world != null)
                {
                    var chronicleSystem = world.GetExistingSystemManaged<ProceduralWorld.Simulation.Systems.LivingChronicleSystem>();
                    if (chronicleSystem != null)
                    {
                        _isInitialized = true;
                        if (showStartupMessage)
                        {
                            Debug.Log("[ChronicleUIManager] Living Chronicle System detected - Chronicle Reader is ready!");
                        }
                    }
                }
            }
        }
        
        private void SetupChronicleReader()
        {
            // Check if we already have one
            _chronicleReader = FindFirstObjectByType<ChronicleReaderUI>();
            
            if (_chronicleReader == null)
            {
                // Create a new GameObject with the ChronicleReaderUI component
                var chronicleReaderGO = new GameObject("ChronicleReader");
                chronicleReaderGO.transform.SetParent(this.transform);
                
                _chronicleReader = chronicleReaderGO.AddComponent<ChronicleReaderUI>();
                
                Debug.Log("[ChronicleUIManager] Created Chronicle Reader UI");
            }
            else
            {
                Debug.Log("[ChronicleUIManager] Found existing Chronicle Reader UI");
            }
        }
        
        public ChronicleReaderUI GetChronicleReader()
        {
            return _chronicleReader;
        }
        
        public bool IsChronicleSystemReady()
        {
            return _isInitialized;
        }
        
        // Public method to open the chronicle reader
        public void OpenChronicleReader()
        {
            if (_chronicleReader != null)
            {
                _chronicleReader.ToggleChronicleReader();
            }
            else
            {
                Debug.LogWarning("[ChronicleUIManager] Chronicle Reader not available");
            }
        }
    }
} 