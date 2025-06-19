using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;
using ProceduralWorld.Simulation.Core;
using ProceduralWorld.Simulation.Components;
using UnityEngine;
using ProceduralWorld.Simulation.Utils;

namespace ProceduralWorld.Simulation.Utils
{
    [BurstCompile]
    public static class SimulationUtils
    {
        [BurstCompile]
        public static float GenerateNoise(ref float2 position, float scale, int seed)
        {
            return noise.snoise(position * scale + seed);
        }

        [BurstCompile]
        public static float CalculateEnvironmentalImpact(ref WorldTerrainData terrain, ref float3 position)
        {
            int index = (int)position.z * terrain.Resolution + (int)position.x;
            return terrain.HeightMap[index] * terrain.TemperatureMap[index] * terrain.MoistureMap[index];
        }

        [BurstCompile]
        public static float CalculateReligiousInfluence(float population, float stability)
        {
            return population * stability;
        }

        [BurstCompile]
        public static float CalculateEventSignificance(float population, float stability)
        {
            return population * stability;
        }

        [BurstCompile]
        public static float CalculateResourceValue(Core.ResourceType resourceType, float amount)
        {
            float baseValue = resourceType switch
            {
                Core.ResourceType.Wood => 1f,
                Core.ResourceType.Stone => 2f,
                Core.ResourceType.Metal => 3f,
                Core.ResourceType.Food => 1.5f,
                Core.ResourceType.Water => 1f,
                _ => 1f
            };
            return baseValue * amount;
        }

        [BurstCompile]
        public static float CalculateResourceAbundance(Core.BiomeType biome, Core.ResourceType resource)
        {
            return biome switch
            {
                Core.BiomeType.Forest => resource == Core.ResourceType.Wood ? 1f : 0.2f,
                Core.BiomeType.Mountains => resource == Core.ResourceType.Stone ? 1f : 0.1f,
                Core.BiomeType.Grassland => resource == Core.ResourceType.Food ? 1f : 0.3f,
                _ => 0.1f
            };
        }

        [BurstCompile]
        public static float CalculateInteractionProbability(float affinity, float distance)
        {
            return math.max(0f, 1f - (distance / affinity));
        }

        [BurstCompile]
        public static Core.BiomeType DetermineBiome(float height, float temperature, float moisture)
        {
            if (height < 0.2f) return Core.BiomeType.Ocean;
            if (height < 0.3f) return Core.BiomeType.Beach;
            if (height < 0.4f)
            {
                if (moisture < 0.3f) return Core.BiomeType.Desert;
                if (moisture < 0.6f) return Core.BiomeType.Grassland;
                return Core.BiomeType.Forest;
            }
            if (height < 0.6f)
            {
                if (temperature < 0.3f) return Core.BiomeType.Taiga;
                if (moisture < 0.4f) return Core.BiomeType.Savanna;
                return Core.BiomeType.Forest;
            }
            if (height < 0.8f) return Core.BiomeType.Mountains;
            return Core.BiomeType.Snow;
        }

        public static float CalculateDistance(float3 pos1, float3 pos2)
        {
            return math.distance(pos1, pos2);
        }

        public static float CalculateInfluence(float distance, float baseInfluence)
        {
            return baseInfluence * math.exp(-distance * 0.1f);
        }

        public static float CalculateInteractionStrength(float distance, float baseStrength)
        {
            return baseStrength * math.exp(-distance * 0.05f);
        }

        public static Core.BiomeType GetBiomeType(float height, float temperature, float moisture)
        {
            // Water biomes
            if (height < 0.1f)
            {
                if (moisture > 0.8f) return Core.BiomeType.Ocean;
                if (moisture > 0.6f) return Core.BiomeType.Sea;
                if (moisture > 0.4f) return Core.BiomeType.Coast;
                return Core.BiomeType.Beach;
            }

            // River and water features
            if (height < 0.2f)
            {
                if (moisture > 0.9f) return Core.BiomeType.River;
                if (moisture > 0.8f) return Core.BiomeType.Lake;
                if (moisture > 0.7f) return Core.BiomeType.Swamp;
                if (moisture > 0.6f) return Core.BiomeType.Marsh;
                if (moisture > 0.5f) return Core.BiomeType.Wetland;
                if (moisture > 0.4f) return Core.BiomeType.Delta;
                if (moisture > 0.3f) return Core.BiomeType.Estuary;
                return Core.BiomeType.Beach;
            }

            // Land biomes
            if (height < 0.4f)
            {
                if (temperature < 0.2f) return Core.BiomeType.Tundra;
                if (temperature < 0.4f)
                {
                    if (moisture < 0.3f) return Core.BiomeType.Plains;
                    if (moisture < 0.6f) return Core.BiomeType.Grassland;
                    return Core.BiomeType.Forest;
                }
                if (temperature < 0.6f)
                {
                    if (moisture < 0.3f) return Core.BiomeType.Desert;
                    if (moisture < 0.6f) return Core.BiomeType.Savanna;
                    return Core.BiomeType.Forest;
                }
                if (temperature < 0.8f)
                {
                    if (moisture < 0.3f) return Core.BiomeType.Desert;
                    if (moisture < 0.6f) return Core.BiomeType.Grassland;
                    return Core.BiomeType.Forest;
                }
                return Core.BiomeType.Desert;
            }

            // Mountain biomes
            if (height < 0.6f)
            {
                if (temperature < 0.3f) return Core.BiomeType.Taiga;
                if (moisture < 0.4f) return Core.BiomeType.Savanna;
                return Core.BiomeType.Forest;
            }

            // High mountain biomes
            if (height < 0.8f)
            {
                if (temperature < 0.2f) return Core.BiomeType.Snow;
                return Core.BiomeType.Mountains;
            }

            return Core.BiomeType.Snow;
        }

        public static float CalculateTerrainHeight(float x, float z, float scale, float persistence, float lacunarity, int octaves)
        {
            float amplitude = 1f;
            float frequency = 1f;
            float noiseHeight = 0f;
            float amplitudeSum = 0f;

            for (int i = 0; i < octaves; i++)
            {
                float sampleX = x * frequency;
                float sampleZ = z * frequency;

                float perlinValue = noise.cnoise(new float2(sampleX, sampleZ)) * 2f - 1f;
                noiseHeight += perlinValue * amplitude;
                amplitudeSum += amplitude;

                amplitude *= persistence;
                frequency *= lacunarity;
            }

            return noiseHeight / amplitudeSum;
        }

        public static float GetTerrainHeight(float x, float z, NativeArray<float> heightMap, int resolution)
        {
            int index = (int)z * resolution + (int)x;
            return heightMap[index];
        }

        [BurstCompile]
        public static Core.ResourceType DetermineResourceType(Core.BiomeType biome)
        {
            switch (biome)
            {
                case Core.BiomeType.Forest: return Core.ResourceType.Wood;
                case Core.BiomeType.Mountains: return Core.ResourceType.Stone;
                case Core.BiomeType.Desert: return Core.ResourceType.Gold;
                case Core.BiomeType.Ocean: return Core.ResourceType.Fish;
                case Core.BiomeType.Grassland: return Core.ResourceType.Food;
                default: return Core.ResourceType.None;
            }
        }

        public static float GetBiomeHeight(Core.BiomeType biome)
        {
            switch (biome)
            {
                case Core.BiomeType.Ocean:
                    return 0.0f;
                case Core.BiomeType.Mountains:
                    return 1.0f;
                case Core.BiomeType.Forest:
                    return 0.3f;
                case Core.BiomeType.Grassland:
                    return 0.2f;
                case Core.BiomeType.Desert:
                    return 0.1f;
                case Core.BiomeType.Tundra:
                    return 0.15f;
                case Core.BiomeType.Swamp:
                    return 0.05f;
                case Core.BiomeType.Jungle:
                    return 0.25f;
                default:
                    return 0.0f;
            }
        }

        public static float4 GetBiomeColor(Core.BiomeType biome)
        {
            switch (biome)
            {
                case Core.BiomeType.Ocean:
                    return new float4(0.1f, 0.3f, 0.8f, 1.0f);
                case Core.BiomeType.Mountains:
                    return new float4(0.5f, 0.5f, 0.5f, 1.0f);
                case Core.BiomeType.Forest:
                    return new float4(0.1f, 0.6f, 0.1f, 1.0f);
                case Core.BiomeType.Grassland:
                    return new float4(0.4f, 0.8f, 0.2f, 1.0f);
                case Core.BiomeType.Desert:
                    return new float4(0.9f, 0.8f, 0.4f, 1.0f);
                case Core.BiomeType.Tundra:
                    return new float4(0.8f, 0.8f, 0.9f, 1.0f);
                case Core.BiomeType.Swamp:
                    return new float4(0.3f, 0.5f, 0.2f, 1.0f);
                case Core.BiomeType.Jungle:
                    return new float4(0.2f, 0.7f, 0.2f, 1.0f);
                default:
                    return new float4(0.5f, 0.5f, 0.5f, 1.0f);
            }
        }

        [BurstCompile]
        public static Core.ResourceType GetResourceForBiome(Core.BiomeType biome)
        {
            switch (biome)
            {
                case Core.BiomeType.Forest:
                    return Core.ResourceType.Wood;
                case Core.BiomeType.Mountains:
                    return Core.ResourceType.Stone;
                case Core.BiomeType.Grassland:
                    return Core.ResourceType.Food;
                case Core.BiomeType.Desert:
                    return Core.ResourceType.Gold;
                case Core.BiomeType.Tundra:
                    return Core.ResourceType.Iron;
                case Core.BiomeType.Swamp:
                    return Core.ResourceType.Herbs;
                case Core.BiomeType.Jungle:
                    return Core.ResourceType.Rubber;
                default:
                    return Core.ResourceType.None;
            }
        }

        [BurstCompile]
        public static float GetResourceValue(Core.ResourceType resource)
        {
            return resource switch
            {
                Core.ResourceType.Wood => 1f,
                Core.ResourceType.Stone => 2f,
                Core.ResourceType.Metal => 3f,
                Core.ResourceType.Food => 1.5f,
                Core.ResourceType.Water => 1f,
                _ => 1f
            };
        }

        public static float GetBiomeInfluence(Core.BiomeType biome)
        {
            switch (biome)
            {
                case Core.BiomeType.Forest:
                    return 0.8f;
                case Core.BiomeType.Mountains:
                    return 0.6f;
                case Core.BiomeType.Grassland:
                    return 1.0f;
                case Core.BiomeType.Desert:
                    return 0.4f;
                case Core.BiomeType.Tundra:
                    return 0.3f;
                case Core.BiomeType.Swamp:
                    return 0.5f;
                case Core.BiomeType.Jungle:
                    return 0.7f;
                default:
                    return 0.0f;
            }
        }

        [BurstCompile]
        public static float GetResourceInfluence(Core.ResourceType resource)
        {
            return resource switch
            {
                Core.ResourceType.Wood => 0.5f,
                Core.ResourceType.Stone => 0.7f,
                Core.ResourceType.Metal => 0.9f,
                Core.ResourceType.Food => 0.6f,
                Core.ResourceType.Water => 0.4f,
                _ => 0.3f
            };
        }

        [BurstCompile]
        public static float GetBiomeResourceMultiplier(Core.BiomeType biome, Core.ResourceType resource)
        {
            return biome switch
            {
                Core.BiomeType.Forest => resource == Core.ResourceType.Wood ? 2f : 0.5f,
                Core.BiomeType.Mountains => resource == Core.ResourceType.Stone ? 2f : 0.3f,
                Core.BiomeType.Grassland => resource == Core.ResourceType.Food ? 2f : 0.7f,
                _ => 0.5f
            };
        }
    }
} 