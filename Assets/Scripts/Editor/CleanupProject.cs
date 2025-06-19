using UnityEngine;
using UnityEditor;
using System.IO;

namespace ProceduralWorld.Simulation.Editor
{
    public class CleanupProject : EditorWindow
    {
        [MenuItem("Window/Simulation/Cleanup Project")]
        public static void ShowWindow()
        {
            GetWindow<CleanupProject>("Project Cleanup");
        }

        private void OnGUI()
        {
            GUILayout.Label("Project Cleanup Tools", EditorStyles.boldLabel);

            if (GUILayout.Button("Clean Library and Temp Folders"))
            {
                CleanFolders();
            }

            if (GUILayout.Button("Reimport All Assets"))
            {
                AssetDatabase.Refresh();
            }
        }

        private void CleanFolders()
        {
            string[] foldersToClean = new string[]
            {
                "Library",
                "Temp",
                "obj"
            };

            foreach (string folder in foldersToClean)
            {
                string path = Path.Combine(Application.dataPath, "..", folder);
                if (Directory.Exists(path))
                {
                    try
                    {
                        Directory.Delete(path, true);
                        Debug.Log($"Deleted {folder} folder");
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"Failed to delete {folder} folder: {e.Message}");
                    }
                }
            }

            EditorApplication.Exit(0);
        }
    }
} 