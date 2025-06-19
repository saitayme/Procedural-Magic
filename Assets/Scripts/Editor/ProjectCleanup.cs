#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

namespace ProceduralWorld.Simulation.Editor
{
    public class ProjectCleanup : EditorWindow
    {
        [MenuItem("Window/Simulation/Project Cleanup")]
        public static void ShowWindow()
        {
            GetWindow<ProjectCleanup>("Project Cleanup");
        }

        private void OnGUI()
        {
            GUILayout.Label("Project Cleanup Tools", EditorStyles.boldLabel);

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "This will clean up the project and prepare it for a fresh build. " +
                "Make sure to save any unsaved work before proceeding.",
                MessageType.Info
            );

            EditorGUILayout.Space();
            if (GUILayout.Button("Clean Project and Rebuild"))
            {
                CleanProject();
            }

            EditorGUILayout.Space();
            if (GUILayout.Button("Reimport All Assets"))
            {
                AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            }
        }

        private void CleanProject()
        {
            string[] foldersToClean = new string[]
            {
                "Library",
                "Temp",
                "obj",
                "ScriptAssemblies"
            };

            bool shouldClose = false;
            foreach (string folder in foldersToClean)
            {
                string path = Path.Combine(Application.dataPath, "..", folder);
                if (Directory.Exists(path))
                {
                    try
                    {
                        Directory.Delete(path, true);
                        Debug.Log($"Deleted {folder} folder");
                        shouldClose = true;
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"Failed to delete {folder} folder: {e.Message}");
                    }
                }
            }

            if (shouldClose)
            {
                EditorApplication.Exit(0);
            }
        }
    }
}
#endif 