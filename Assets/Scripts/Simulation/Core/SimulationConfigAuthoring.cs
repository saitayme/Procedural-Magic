using UnityEngine;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Collections;
using ProceduralWorld.Simulation.Components;

namespace ProceduralWorld.Simulation.Core
{
    public class SimulationConfigAuthoring : MonoBehaviour
    {
        [Header("World Settings")]
        public int WorldSize = 256;
        public float TerrainScale = 1.0f;
        public float HeightScale = 1.0f;
        public float TemperatureScale = 1.0f;
        public float MoistureScale = 1.0f;

        [Header("System Settings")]
        public bool EnableTerrainSystem = true;
        public bool EnableCivilizationSystem = true;
        public bool EnableReligionSystem = true;
        public bool EnableEconomySystem = true;
        public bool EnableCursorSystem = true;
        public bool EnableHistorySystem = true;
        public bool EnableResourceSystem = true;
        public bool EnableVisualizationSystem = true;

        [Header("Resource Settings")]
        public List<ResourceType> ResourceTypes = new List<ResourceType>();

        public class Baker : Baker<SimulationConfigAuthoring>
        {
            public override void Bake(SimulationConfigAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                
                // Create a NativeArray for resource types
                var resourceTypesArray = new NativeArray<ResourceType>(authoring.ResourceTypes.Count, Allocator.Persistent);
                for (int i = 0; i < authoring.ResourceTypes.Count; i++)
                {
                    resourceTypesArray[i] = authoring.ResourceTypes[i];
                }

                AddComponent(entity, new SimulationConfig
                {
                    WorldSize = authoring.WorldSize,
                    TerrainScale = authoring.TerrainScale,
                    HeightScale = authoring.HeightScale,
                    TemperatureScale = authoring.TemperatureScale,
                    MoistureScale = authoring.MoistureScale,
                    EnableTerrainSystem = authoring.EnableTerrainSystem,
                    EnableCivilizationSystem = authoring.EnableCivilizationSystem,
                    EnableReligionSystem = authoring.EnableReligionSystem,
                    EnableEconomySystem = authoring.EnableEconomySystem,
                    EnableCursorSystem = authoring.EnableCursorSystem,
                    EnableHistorySystem = authoring.EnableHistorySystem,
                    EnableResourceSystem = authoring.EnableResourceSystem,
                    EnableVisualizationSystem = authoring.EnableVisualizationSystem,
                    ResourceTypes = resourceTypesArray
                });
            }
        }
    }
} 