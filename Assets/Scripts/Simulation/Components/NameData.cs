using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;

namespace ProceduralWorld.Simulation.Components
{
    [BurstCompile]
    public struct NameData : IComponentData
    {
        public FixedString128Bytes Name;
        public FixedString128Bytes BiomeName;
        public FixedString128Bytes MountainName;
        public FixedString128Bytes RiverName;
        public FixedString128Bytes ForestName;
        public FixedString128Bytes DesertName;
        public FixedString128Bytes OceanName;
        public float3 Position;
        public NameType Type;
        public float Significance;
        public Entity SourceEntityId;
    }

    public enum NameType
    {
        None,
        Civilization,
        City,
        Region,
        Landmark,
        River,
        Mountain,
        Forest,
        Desert,
        Ocean,
        Island,
        Continent,
        Star,
        Planet,
        Moon,
        Galaxy,
        Universe,
        Biome,
        Monument,
        Structure,
        Ruin
    }
} 