#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using ProceduralWorld.Simulation.Components;
using ProceduralWorld.Simulation.Core;

namespace ProceduralWorld.Simulation.Editor
{
    public class SimulationEditor : EditorWindow
    {
        [MenuItem("Window/Simulation/Debug Window")]
        public static void ShowWindow()
        {
            GetWindow<SimulationEditor>("Simulation Debug");
        }

        private void OnGUI()
        {
            GUILayout.Label("Simulation Debug Tools", EditorStyles.boldLabel);
            
            EditorGUILayout.Space();
            if (GUILayout.Button("Clear All Data"))
            {
                ClearAllData();
            }

            EditorGUILayout.Space();
            if (GUILayout.Button("Reset Simulation"))
            {
                ResetSimulation();
            }
        }

        private void ClearAllData()
        {
            // Implementation for clearing data
            Debug.Log("Clearing all simulation data...");
        }

        private void ResetSimulation()
        {
            // Implementation for resetting simulation
            Debug.Log("Resetting simulation...");
        }
    }
}
#endif 