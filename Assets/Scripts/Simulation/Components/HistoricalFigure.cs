using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;
using ProceduralWorld.Simulation.Core;

namespace ProceduralWorld.Simulation.Components
{
    [BurstCompile]
    public struct HistoricalFigure : IComponentData
    {
        public FixedString128Bytes Name;
        public FixedString128Bytes Title;
        public FixedString512Bytes Description;
        public Entity Civilization;
        public float BirthDate;
        public float DeathDate;
        public int BirthYear;
        public int DeathYear;
        public float3 Location;
        public float Influence;
        public float Legacy;
        public float Achievement;
        public float Controversy;
        public float Innovation;
        public float Tradition;
        public float Progress;
        public float Regression;
        public int Id;
    }
} 