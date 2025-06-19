using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;

namespace ProceduralWorld.Simulation.Components
{
    public struct TerritoryData : IComponentData
    {
        public Entity OwnerCivilization;
        public float3 Position;
        public float ControlRadius;
        public FixedString128Bytes TerritoryName;
        public TerritoryType Type;
        public float DefenseStrength;
        public float Population;
        public float Wealth;
        public bool IsRuined;
        public FixedString128Bytes OriginalName; // For ruins
        public Entity OriginalOwner; // For ruins
    }

    public enum TerritoryType
    {
        City,
        Monument,
        Temple,
        Academy,
        Marketplace,
        Palace,
        Wonder,
        Ruins
    }
} 