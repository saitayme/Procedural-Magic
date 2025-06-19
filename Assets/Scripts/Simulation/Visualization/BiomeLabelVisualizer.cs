using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using ProceduralWorld.Simulation.Components;
using TMPro;
using System.Collections.Generic;

namespace ProceduralWorld.Simulation.Visualization
{
    public class BiomeLabelVisualizer : MonoBehaviour
    {
        [SerializeField] private UnityEngine.Camera mainCamera;
        [SerializeField] private GameObject labelPrefab;
        private EntityManager _entityManager;
        private Dictionary<int, GameObject> _labels = new();

        void Start()
        {
            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            if (mainCamera == null)
                mainCamera = UnityEngine.Camera.main;
        }

        void Update()
        {
            if (_entityManager == null || labelPrefab == null) return;

            var terrainQuery = _entityManager.CreateEntityQuery(typeof(WorldTerrainData));
            if (!terrainQuery.HasSingleton<WorldTerrainData>()) return;
            var terrain = _entityManager.GetComponentData<WorldTerrainData>(terrainQuery.GetSingletonEntity());
            
            // Check if BiomeMap is initialized
            if (!terrain.BiomeMap.IsCreated || terrain.BiomeMap.Length == 0)
            {
                Debug.Log("[BiomeLabelVisualizer] BiomeMap not initialized yet");
                return;
            }

            var biomeMap = terrain.BiomeMap;
            int resolution = terrain.Resolution;
            float worldSize = terrain.WorldSize;
            float scaleX = worldSize / (resolution - 1);
            float scaleZ = worldSize / (resolution - 1);

            // Remove old labels
            foreach (var label in _labels.Values)
                Destroy(label);
            _labels.Clear();

            // Find biome regions and place labels strategically
            var biomeRegions = new Dictionary<ProceduralWorld.Simulation.Core.BiomeType, List<Vector3>>();
            
            // Sample the map in a grid pattern to find biome centers
            int sampleStep = resolution / 8; // Sample every 8th point for better distribution
            
            for (int y = sampleStep; y < resolution - sampleStep; y += sampleStep)
            {
                for (int x = sampleStep; x < resolution - sampleStep; x += sampleStep)
                {
                    int index = y * resolution + x;
                    var biome = biomeMap[index];
                    
                    if (biome == ProceduralWorld.Simulation.Core.BiomeType.None) continue;
                    
                    // Convert to world position (centered around origin)
                    float xPos = (x * scaleX) - (worldSize * 0.5f);
                    float zPos = (y * scaleZ) - (worldSize * 0.5f);
                    float height = terrain.HeightMap[index] * 100f; // Match height scale
                    var pos = new Vector3(xPos, height + 15f, zPos); // Raise labels above terrain
                    
                    if (!biomeRegions.ContainsKey(biome))
                        biomeRegions[biome] = new List<Vector3>();
                    
                    biomeRegions[biome].Add(pos);
                }
            }

            // Place one label per biome type at the most central location
            foreach (var kvp in biomeRegions)
            {
                var biome = kvp.Key;
                var positions = kvp.Value;
                
                if (positions.Count == 0) continue;
                
                // Find the most central position for this biome
                Vector3 center = Vector3.zero;
                foreach (var pos in positions)
                    center += pos;
                center /= positions.Count;
                
                // Find the position closest to the calculated center
                Vector3 bestPosition = positions[0];
                float bestDistance = Vector3.Distance(center, bestPosition);
                
                foreach (var pos in positions)
                {
                    float distance = Vector3.Distance(center, pos);
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        bestPosition = pos;
                    }
                }
                
                // Create a simple 3D text object
                var labelObj = new GameObject($"BiomeLabel_{biome}");
                labelObj.transform.position = bestPosition;
                
                // Add 3D TextMesh component
                var textMesh = labelObj.AddComponent<TextMesh>();
                textMesh.text = biome.ToString();
                textMesh.fontSize = 40; // Larger for better visibility
                textMesh.color = GetBiomeColor(biome);
                textMesh.anchor = TextAnchor.MiddleCenter;
                
                // Make it face the camera
                if (mainCamera != null)
                {
                    labelObj.transform.LookAt(labelObj.transform.position + mainCamera.transform.rotation * Vector3.forward,
                                            mainCamera.transform.rotation * Vector3.up);
                }
                
                _labels[(int)biome] = labelObj;
                
                Debug.Log($"[BiomeLabelVisualizer] Placed {biome} label at {bestPosition} (from {positions.Count} candidates)");
            }
        }

        private Color GetBiomeColor(ProceduralWorld.Simulation.Core.BiomeType biome)
        {
            // Implement your logic to determine the color based on the biome type
            // For example, you can use a switch statement or a dictionary to map biomes to colors
            switch (biome)
            {
                case ProceduralWorld.Simulation.Core.BiomeType.Forest:
                    return Color.green;
                case ProceduralWorld.Simulation.Core.BiomeType.Desert:
                    return Color.yellow;
                case ProceduralWorld.Simulation.Core.BiomeType.Mountains:
                    return Color.gray;
                case ProceduralWorld.Simulation.Core.BiomeType.Ocean:
                    return Color.blue;
                case ProceduralWorld.Simulation.Core.BiomeType.Coast:
                    return Color.cyan;
                case ProceduralWorld.Simulation.Core.BiomeType.Plains:
                    return Color.green;
                case ProceduralWorld.Simulation.Core.BiomeType.Rainforest:
                    return new Color(0f, 0.5f, 0f); // Dark green
                case ProceduralWorld.Simulation.Core.BiomeType.Tundra:
                    return Color.white;
                case ProceduralWorld.Simulation.Core.BiomeType.Swamp:
                    return new Color(0.3f, 0.4f, 0.2f);
                default:
                    return Color.white;
            }
        }
    }
} 