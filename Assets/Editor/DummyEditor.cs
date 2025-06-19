using UnityEngine;
using UnityEditor;

namespace ProceduralWorld.Simulation.Editor
{
    public class DummyEditor : EditorWindow
    {
        [MenuItem("Window/Simulation/Dummy")]
        public static void ShowWindow()
        {
            GetWindow<DummyEditor>("Dummy");
        }

        private void OnGUI()
        {
            GUILayout.Label("Dummy Editor Window", EditorStyles.boldLabel);
        }
    }
} 