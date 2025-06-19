using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;
using UnityEditor;
using UnityEngine;
using ProceduralWorld.Simulation.Core;
using ProceduralWorld.Simulation.Components;
using ProceduralWorld.Simulation.Utils;

namespace ProceduralWorld.Simulation.Editor
{
    public class SimulationDebugWindow : EditorWindow
    {
        private EntityQuery _configQuery;
        private EntityQuery _statsQuery;
        private bool _isInitialized;
        private World _world;

        [MenuItem("Window/Simulation Debug")]
        public static void ShowWindow()
        {
            GetWindow<SimulationDebugWindow>("Simulation Debug");
        }

        private void OnEnable()
        {
            _world = World.DefaultGameObjectInjectionWorld;
            if (_world != null)
            {
                _configQuery = _world.EntityManager.CreateEntityQuery(
                    ComponentType.ReadOnly<SimulationConfig>()
                );
                _statsQuery = _world.EntityManager.CreateEntityQuery(
                    ComponentType.ReadOnly<SimulationStats>()
                );
                _isInitialized = true;
            }
        }

        private void OnGUI()
        {
            if (!_isInitialized || _world == null)
            {
                EditorGUILayout.HelpBox("No active simulation world found.", MessageType.Warning);
                return;
            }

            GUILayout.Label("Simulation Debug", EditorStyles.boldLabel);

            var config = _world.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<SimulationConfig>()).GetSingleton<SimulationConfig>();
            var stats = _world.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<SimulationStats>()).GetSingleton<SimulationStats>();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Simulation Time", $"{stats.SimulationTime:F2}s");
            EditorGUILayout.LabelField("Civilizations", stats.CivilizationCount.ToString());
            EditorGUILayout.LabelField("Resources", stats.ResourceCount.ToString());
            EditorGUILayout.LabelField("Structures", stats.StructureCount.ToString());
            EditorGUILayout.LabelField("Events", stats.EventCount.ToString());
        }
    }
} 