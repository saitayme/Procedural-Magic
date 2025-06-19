using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;
using ProceduralWorld.Simulation.Core;
using System.Collections.Generic;

namespace ProceduralWorld.Simulation.Visualization
{
    public class TerrainVisualizationComponent : MonoBehaviour
    {
        public int Resolution = 256;
        public float WorldSize = 1000f;
        public float HeightScale = 100f;
        public float TemperatureScale = 1f;
        public float MoistureScale = 1f;
        public float ResourceScale = 1f;
        public float ResourceTypeScale = 1f;
        public float BiomeScale = 1f;
        public float Smoothness = 0.5f;
        public float Metallic = 0f;
        public float Glossiness = 0.5f;
        public Color BaseColor = new Color(0.5f, 0.5f, 0.5f, 1f);
        public Color TemperatureColor = new Color(1f, 0f, 0f, 1f);
        public Color MoistureColor = new Color(0f, 0f, 1f, 1f);
        public Color ResourceColor = new Color(0f, 1f, 0f, 1f);
        public Color ResourceTypeColor = new Color(1f, 1f, 0f, 1f);
        public Color BiomeColor = new Color(0f, 1f, 1f, 1f);

        private Mesh _mesh;
        private Material _material;
        private bool _isInitialized;
        public bool IsInitialized => _isInitialized;

        public void Initialize()
        {
            Debug.Log("[TerrainVisualizationComponent] Initializing component");
            if (_isInitialized)
            {
                Debug.Log("[TerrainVisualizationComponent] Component already initialized");
                return;
            }

            try
            {
                // Create mesh if needed
                if (_mesh == null)
                {
                    Debug.Log("[TerrainVisualizationComponent] Creating new mesh");
                    _mesh = new Mesh();
                    _mesh.name = "TerrainMesh";
                    _mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                }

                // Create material if needed
                if (_material == null)
                {
                    Debug.Log("[TerrainVisualizationComponent] Creating new material");
                    _material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    if (_material == null)
                    {
                        Debug.LogError("[TerrainVisualizationComponent] URP Lit shader not found. Please ensure URP is properly set up in your project.");
                        // Fallback to standard shader if URP is not available
                        _material = new Material(Shader.Find("Standard"));
                        if (_material == null)
                        {
                            Debug.LogError("[TerrainVisualizationComponent] Standard shader not found. Please ensure your project has basic shaders available.");
                            return;
                        }
                    }
                    
                    // Configure material properties
                    _material.SetFloat("_Smoothness", Smoothness);
                    _material.SetFloat("_Metallic", Metallic);
                    _material.SetFloat("_Glossiness", Glossiness);
                    _material.SetColor("_BaseColor", BaseColor);
                    _material.SetColor("_Color", BaseColor);
                    
                    // Enable shadows and lighting
                    _material.EnableKeyword("_RECEIVE_SHADOWS_ON");
                    _material.EnableKeyword("_MAIN_LIGHT_SHADOWS");
                    _material.EnableKeyword("_MAIN_LIGHT_SHADOWS_CASCADE");
                    _material.EnableKeyword("_ADDITIONAL_LIGHTS_VERTEX");
                    
                    // Set rendering mode
                    _material.SetFloat("_Surface", 0); // 0 = Opaque
                    _material.SetFloat("_Blend", 0);
                    _material.SetFloat("_AlphaClip", 0);
                    _material.renderQueue = 2000; // Opaque queue
                }

                // Set up mesh renderer
                var meshFilter = GetComponent<MeshFilter>();
                if (meshFilter == null)
                {
                    Debug.Log("[TerrainVisualizationComponent] Adding MeshFilter component");
                    meshFilter = gameObject.AddComponent<MeshFilter>();
                }
                meshFilter.mesh = _mesh;

                var meshRenderer = GetComponent<MeshRenderer>();
                if (meshRenderer == null)
                {
                    Debug.Log("[TerrainVisualizationComponent] Adding MeshRenderer component");
                    meshRenderer = gameObject.AddComponent<MeshRenderer>();
                }
                meshRenderer.material = _material;
                meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                meshRenderer.receiveShadows = true;

                // Position the terrain at the origin
                transform.position = Vector3.zero;
                transform.rotation = Quaternion.identity;
                transform.localScale = Vector3.one;

                _isInitialized = true;
                Debug.Log("[TerrainVisualizationComponent] Initialization complete");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[TerrainVisualizationComponent] Error during initialization: {e.Message}\n{e.StackTrace}");
                _isInitialized = false;
            }
        }

        public void UpdateTerrainMesh(
            NativeArray<float> heightMap,
            NativeArray<float> temperatureMap,
            NativeArray<float> moistureMap,
            NativeArray<ProceduralWorld.Simulation.Core.BiomeType> biomeMap,
            NativeArray<float> resourceMap,
            NativeArray<ProceduralWorld.Simulation.Core.ResourceType> resourceTypeMap)
        {
            if (HeightScale < 1f || HeightScale > 200f || WorldSize < 10f || WorldSize > 10000f)
            {
                Debug.LogWarning($"[TerrainVisualizationComponent] HeightScale ({HeightScale}) or WorldSize ({WorldSize}) is outside recommended human-scale range. Adjust SimulationConfig for realistic scale.");
            }

            if (!_isInitialized)
            {
                Debug.Log("[TerrainVisualizationComponent] Component not initialized, initializing now");
                Initialize();
            }

            try
            {
                Debug.Log("[TerrainVisualizationComponent] Updating terrain mesh");
                
                // Log height map range for debugging
                float minHeight = float.MaxValue;
                float maxHeight = float.MinValue;
                for (int i = 0; i < heightMap.Length; i++)
                {
                    minHeight = math.min(minHeight, heightMap[i]);
                    maxHeight = math.max(maxHeight, heightMap[i]);
                }
                Debug.Log($"[TerrainVisualizationComponent] HeightMap min: {minHeight}, max: {maxHeight}");

                // Create vertices and triangles
                var vertices = new List<Vector3>();
                var triangles = new List<int>();
                var uvs = new List<Vector2>();
                var colors = new List<Color>();

                float heightScale = HeightScale; // Use the component's value, which is set from config
                float worldSize = WorldSize; // Use the component's value, which is set from config
                float cellSize = worldSize / (Resolution - 1);

                // Generate mesh data
                for (int z = 0; z < Resolution; z++)
                {
                    for (int x = 0; x < Resolution; x++)
                    {
                        int index = z * Resolution + x;
                        float height = heightMap[index] * heightScale;
                        
                        // Calculate world position
                        float xPos = (x - Resolution / 2) * cellSize;
                        float zPos = (z - Resolution / 2) * cellSize;
                        
                        vertices.Add(new Vector3(xPos, height, zPos));
                        uvs.Add(new Vector2(x / (float)(Resolution - 1), z / (float)(Resolution - 1)));
                        
                        // Set color based on biome
                        Color color = GetBiomeColor(biomeMap[index]);
                        colors.Add(color);

                        // Create triangles
                        if (x < Resolution - 1 && z < Resolution - 1)
                        {
                            int topLeft = z * Resolution + x;
                            int topRight = topLeft + 1;
                            int bottomLeft = (z + 1) * Resolution + x;
                            int bottomRight = bottomLeft + 1;

                            triangles.Add(topLeft);
                            triangles.Add(bottomLeft);
                            triangles.Add(topRight);

                            triangles.Add(topRight);
                            triangles.Add(bottomLeft);
                            triangles.Add(bottomRight);
                        }
                    }
                }

                // Update mesh
                _mesh.Clear();
                _mesh.SetVertices(vertices);
                _mesh.SetTriangles(triangles, 0);
                _mesh.SetUVs(0, uvs);
                _mesh.SetColors(colors);
                _mesh.RecalculateNormals();
                _mesh.RecalculateBounds();

                // Update transform
                transform.position = Vector3.zero;
                transform.rotation = Quaternion.identity;
                transform.localScale = Vector3.one;

                Debug.Log($"[TerrainVisualizationComponent] Mesh update complete. Bounds: {_mesh.bounds}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[TerrainVisualizationComponent] Error updating terrain mesh: {e.Message}\n{e.StackTrace}");
            }
        }

        private Color GetBiomeColor(ProceduralWorld.Simulation.Core.BiomeType biome)
        {
            Color color;

            // Set base color based on biome
            switch (biome)
            {
                case ProceduralWorld.Simulation.Core.BiomeType.Desert:
                    color = new Color(0.76f, 0.7f, 0.5f); // Sandy color
                    break;
                case ProceduralWorld.Simulation.Core.BiomeType.Grassland:
                    color = new Color(0.5f, 0.8f, 0.3f); // Light green
                    break;
                case ProceduralWorld.Simulation.Core.BiomeType.Savanna:
                    color = new Color(0.8f, 0.7f, 0.3f); // Yellowish green
                    break;
                case ProceduralWorld.Simulation.Core.BiomeType.Tundra:
                    color = new Color(0.8f, 0.8f, 0.9f); // Light blue-gray
                    break;
                case ProceduralWorld.Simulation.Core.BiomeType.Taiga:
                    color = new Color(0.3f, 0.5f, 0.3f); // Dark green
                    break;
                case ProceduralWorld.Simulation.Core.BiomeType.BorealForest:
                    color = new Color(0.2f, 0.4f, 0.2f); // Darker green
                    break;
                case ProceduralWorld.Simulation.Core.BiomeType.TemperateForest:
                    color = new Color(0.3f, 0.6f, 0.3f); // Medium green
                    break;
                case ProceduralWorld.Simulation.Core.BiomeType.TropicalForest:
                    color = new Color(0.2f, 0.7f, 0.2f); // Bright green
                    break;
                case ProceduralWorld.Simulation.Core.BiomeType.Rainforest:
                    color = new Color(0.1f, 0.5f, 0.1f); // Deep green
                    break;
                case ProceduralWorld.Simulation.Core.BiomeType.Forest:
                    color = new Color(0.2f, 0.5f, 0.2f); // Standard green
                    break;
                case ProceduralWorld.Simulation.Core.BiomeType.Mountains:
                    color = new Color(0.6f, 0.6f, 0.6f); // Gray
                    break;
                case ProceduralWorld.Simulation.Core.BiomeType.Ocean:
                    color = new Color(0.0f, 0.2f, 0.5f); // Deep blue
                    break;
                case ProceduralWorld.Simulation.Core.BiomeType.Sea:
                    color = new Color(0.0f, 0.3f, 0.6f); // Medium blue
                    break;
                case ProceduralWorld.Simulation.Core.BiomeType.Coast:
                    color = new Color(0.0f, 0.4f, 0.7f); // Light blue
                    break;
                case ProceduralWorld.Simulation.Core.BiomeType.River:
                    color = new Color(0.0f, 0.5f, 0.8f); // Bright blue
                    break;
                case ProceduralWorld.Simulation.Core.BiomeType.Lake:
                    color = new Color(0.0f, 0.4f, 0.7f); // Medium blue
                    break;
                case ProceduralWorld.Simulation.Core.BiomeType.Swamp:
                    color = new Color(0.2f, 0.4f, 0.2f); // Dark green
                    break;
                case ProceduralWorld.Simulation.Core.BiomeType.Marsh:
                    color = new Color(0.3f, 0.5f, 0.3f); // Medium green
                    break;
                case ProceduralWorld.Simulation.Core.BiomeType.Wetland:
                    color = new Color(0.4f, 0.6f, 0.4f); // Light green
                    break;
                case ProceduralWorld.Simulation.Core.BiomeType.Delta:
                    color = new Color(0.3f, 0.5f, 0.4f); // Green-blue
                    break;
                case ProceduralWorld.Simulation.Core.BiomeType.Estuary:
                    color = new Color(0.2f, 0.4f, 0.5f); // Blue-green
                    break;
                case ProceduralWorld.Simulation.Core.BiomeType.Lagoon:
                    color = new Color(0.0f, 0.6f, 0.8f); // Turquoise
                    break;
                case ProceduralWorld.Simulation.Core.BiomeType.Bay:
                    color = new Color(0.0f, 0.5f, 0.7f); // Light blue
                    break;
                case ProceduralWorld.Simulation.Core.BiomeType.Gulf:
                    color = new Color(0.0f, 0.4f, 0.6f); // Medium blue
                    break;
                case ProceduralWorld.Simulation.Core.BiomeType.Strait:
                    color = new Color(0.0f, 0.5f, 0.7f); // Light blue
                    break;
                case ProceduralWorld.Simulation.Core.BiomeType.Channel:
                    color = new Color(0.0f, 0.4f, 0.6f); // Medium blue
                    break;
                case ProceduralWorld.Simulation.Core.BiomeType.Fjord:
                    color = new Color(0.0f, 0.3f, 0.5f); // Deep blue
                    break;
                case ProceduralWorld.Simulation.Core.BiomeType.Plains:
                    color = new Color(0.6f, 0.8f, 0.4f); // Light green-yellow
                    break;
                case ProceduralWorld.Simulation.Core.BiomeType.Beach:
                    color = new Color(0.9f, 0.9f, 0.7f); // Sandy color
                    break;
                case ProceduralWorld.Simulation.Core.BiomeType.Jungle:
                    color = new Color(0.1f, 0.6f, 0.1f); // Vibrant green
                    break;
                case ProceduralWorld.Simulation.Core.BiomeType.Snow:
                    color = new Color(0.9f, 0.9f, 0.95f); // White
                    break;
                default:
                    color = BaseColor;
                    break;
            }

            return color;
        }

        private void OnDestroy()
        {
            if (_mesh != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(_mesh);
                }
                else
                {
                    DestroyImmediate(_mesh);
                }
            }

            if (_material != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(_material);
                }
                else
                {
                    DestroyImmediate(_material);
                }
            }
        }
    }
} 