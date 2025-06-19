using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ProceduralWorld.Simulation.UI
{
    /// <summary>
    /// Displays instructions for the organized UI system
    /// This component can be temporarily added to show users how to use the new UI
    /// </summary>
    public class UIInstructions : MonoBehaviour
    {
        [Header("Instruction Settings")]
        [SerializeField] private bool showInstructions = true;
        [SerializeField] private bool showOnlyOnFirstRun = true;
        [SerializeField] private float displayDuration = 10f; // Auto-hide after 10 seconds
        
        private bool _hasBeenShown = false;
        private float _startTime;
        
        void Start()
        {
            _startTime = Time.time;
            
            if (showOnlyOnFirstRun)
            {
                // Check if this is the first time running
                _hasBeenShown = PlayerPrefs.GetInt("UI_Instructions_Shown", 0) == 1;
                if (!_hasBeenShown)
                {
                    PlayerPrefs.SetInt("UI_Instructions_Shown", 1);
                    PlayerPrefs.Save();
                }
            }
        }
        
        void OnGUI()
        {
            if (!showInstructions) return;
            if (showOnlyOnFirstRun && _hasBeenShown) return;
            if (Time.time - _startTime > displayDuration) return;
            
            // Center of screen instruction panel
            float width = 500f;
            float height = 300f;
            float x = (Screen.width - width) / 2;
            float y = (Screen.height - height) / 2;
            
            // Semi-transparent background
            GUI.color = new Color(0, 0, 0, 0.8f);
            GUI.Box(new Rect(x - 10, y - 10, width + 20, height + 20), "");
            GUI.color = Color.white;
            
            GUILayout.BeginArea(new Rect(x, y, width, height));
            
            GUILayout.Label("🎉 UI CLEANED UP! 🎉", new GUIStyle(GUI.skin.label) 
            { 
                fontSize = 20, 
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.green }
            });
            
            GUILayout.Space(10);
            
            GUILayout.Label("The overlapping GUI mess has been organized into clean panels:", GetBoldLabelStyle());
            
            GUILayout.Space(10);
            
            GUILayout.Label("📍 LEFT SIDE PANELS:");
            GUILayout.Label("  🏛️ Simulation Status - View civilizations & stats");
            GUILayout.Label("  🎨 Visualization Controls - Toggle layers & refresh");
            
            GUILayout.Space(5);
            
            GUILayout.Label("📍 RIGHT SIDE PANELS:");
            GUILayout.Label("  🐛 Debug Info - Performance & entity counts");
            GUILayout.Label("  📚 Recent Events - Historical events feed");
            
            GUILayout.Space(5);
            
            GUILayout.Label("📍 BOTTOM CENTER:");
            GUILayout.Label("  ⏱️ Time Controls - Simulation speed slider");
            
            GUILayout.Space(5);
            
            GUILayout.Label("📍 KEYBOARD SHORTCUTS:");
            GUILayout.Label("  F1: Toggle Main UI  |  F2: Toggle Debug");
            GUILayout.Label("  F3: Toggle Visualization  |  F4: Toggle History");
            
            GUILayout.Space(10);
            
            GUILayout.Label("💡 Each panel can be expanded/collapsed with ▼/▶ buttons", GetMiniLabelStyle());
            
            GUILayout.Space(10);
            
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Got it! ✓", GUILayout.Width(100)))
            {
                showInstructions = false;
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            
            GUILayout.EndArea();
        }
        
        [ContextMenu("Show Instructions Again")]
        public void ShowInstructionsAgain()
        {
            showInstructions = true;
            _startTime = Time.time;
            _hasBeenShown = false;
        }
        
        [ContextMenu("Reset First Run Flag")]
        public void ResetFirstRunFlag()
        {
            PlayerPrefs.DeleteKey("UI_Instructions_Shown");
            PlayerPrefs.Save();
            Debug.Log("First run flag reset - instructions will show again on next start");
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
    }
} 