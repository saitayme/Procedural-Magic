using Unity.Entities;
using Unity.Mathematics;
using ProceduralWorld.Simulation.Core;

namespace ProceduralWorld.Simulation.Components
{
    public struct CursorInteractionData : IComponentData
    {
        public Entity TerrainEntity;
        public float CurrentHeight;
        public Core.BiomeType CurrentBiome;
        public float CurrentTemperature;
        public float CurrentMoisture;
        public Core.ResourceType CurrentResource;
    }
} 