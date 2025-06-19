using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using ProceduralWorld.Simulation.Components;
using ProceduralWorld.Simulation.Core;

namespace ProceduralWorld.Simulation.Visualization
{
    [System.Obsolete("Use LayeredTerrainVisualizer instead for better functionality and layered visualization")]
    public class TerrainColorVisualizer : MonoBehaviour
    {
        [Header("Terrain References")]
        public Terrain terrain;
        public Material terrainMaterial;
        
        [Header("Biome Colors")]
        public Color oceanColor = new Color(0.2f, 0.4f, 0.8f, 1f);
        public Color coastColor = new Color(0.8f, 0.8f, 0.6f, 1f);
        public Color plainsColor = new Color(0.4f, 0.7f, 0.3f, 1f);
        public Color forestColor = new Color(0.2f, 0.5f, 0.2f, 1f);
        public Color rainforestColor = new Color(0.1f, 0.4f, 0.1f, 1f);
        public Color mountainColor = new Color(0.6f, 0.6f, 0.6f, 1f);
        public Color desertColor = new Color(0.9f, 0.8f, 0.4f, 1f);
        public Color tundraColor = new Color(0.7f, 0.8f, 0.9f, 1f);
        public Color swampColor = new Color(0.3f, 0.4f, 0.2f, 1f);
        
        private EntityManager _entityManager;
        private Texture2D _biomeTexture;
        private bool _isInitialized = false;

        void Start()
        {
            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            
            if (terrain == null)
                terrain = FindObjectOfType<Terrain>();
                
            if (terrainMaterial == null && terrain != null)
                terrainMaterial = terrain.materialTemplate;
                
            Debug.LogWarning("[TerrainColorVisualizer] This component is obsolete. Use LayeredTerrainVisualizer instead for better functionality and layered visualization.");
        }

        void Update()
        {
            if (_entityManager == null || terrain == null) return;
            
            if (!_isInitialized)
            {
                InitializeTerrainColoring();
            }
        }

        void InitializeTerrainColoring()
        {
            var terrainQuery = _entityManager.CreateEntityQuery(typeof(WorldTerrainData));
            if (!terrainQuery.HasSingleton<WorldTerrainData>()) return;
            
            var terrainData = _entityManager.GetComponentData<WorldTerrainData>(terrainQuery.GetSingletonEntity());
            
            if (!terrainData.BiomeMap.IsCreated || terrainData.BiomeMap.Length == 0)
            {
                Debug.Log("[TerrainColorVisualizer] BiomeMap not initialized yet");
                return;
            }

            int resolution = terrainData.Resolution;
            
            // Create a texture to represent biome colors
            _biomeTexture = new Texture2D(resolution, resolution, TextureFormat.RGB24, false);
            
            var pixels = new Color[resolution * resolution];
            
            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    int index = y * resolution + x;
                    var biome = terrainData.BiomeMap[index];
                    pixels[index] = GetBiomeColor(biome);
                }
            }
            
            _biomeTexture.SetPixels(pixels);
            _biomeTexture.Apply();
            
            // Apply the texture to the terrain material
            if (terrainMaterial != null)
            {
                terrainMaterial.mainTexture = _biomeTexture;
            }
            else
            {
                // Create a simple material if none exists
                terrainMaterial = new Material(Shader.Find("Standard"));
                terrainMaterial.mainTexture = _biomeTexture;
                if (terrain.materialTemplate == null)
                    terrain.materialTemplate = terrainMaterial;
            }
            
            _isInitialized = true;
            Debug.Log($"[TerrainColorVisualizer] Initialized terrain coloring with {resolution}x{resolution} biome texture");
        }

        Color GetBiomeColor(ProceduralWorld.Simulation.Core.BiomeType biome)
        {
            switch (biome)
            {
                case ProceduralWorld.Simulation.Core.BiomeType.Ocean:
                    return oceanColor;
                case ProceduralWorld.Simulation.Core.BiomeType.Coast:
                    return coastColor;
                case ProceduralWorld.Simulation.Core.BiomeType.Plains:
                    return plainsColor;
                case ProceduralWorld.Simulation.Core.BiomeType.Forest:
                    return forestColor;
                case ProceduralWorld.Simulation.Core.BiomeType.Rainforest:
                    return rainforestColor;
                case ProceduralWorld.Simulation.Core.BiomeType.Mountains:
                    return mountainColor;
                case ProceduralWorld.Simulation.Core.BiomeType.Desert:
                    return desertColor;
                case ProceduralWorld.Simulation.Core.BiomeType.Tundra:
                    return tundraColor;
                case ProceduralWorld.Simulation.Core.BiomeType.Swamp:
                    return swampColor;
                default:
                    return Color.magenta; // Debug color for unknown biomes
            }
        }

        void OnDestroy()
        {
            if (_biomeTexture != null)
            {
                DestroyImmediate(_biomeTexture);
            }
        }
    }
} 