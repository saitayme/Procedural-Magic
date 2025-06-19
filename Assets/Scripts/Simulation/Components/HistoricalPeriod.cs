using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;
using ProceduralWorld.Simulation.Core;

namespace ProceduralWorld.Simulation.Components
{
    [BurstCompile]
    public struct HistoricalPeriod : IComponentData
    {
        public int Id;
        public FixedString128Bytes Name;
        public FixedString512Bytes Description;
        public float StartDate;
        public float EndDate;
        public int StartYear;
        public int EndYear;
        public float Significance;
        public float Impact;
        public Entity PrimaryCivilization;
        public FixedString128Bytes Category;
        public FixedString128Bytes SubCategory;
        public float Duration;
        public float Intensity;
        public float Reach;
        public float Legacy;
        public float Controversy;
        public float Innovation;
        public float Tradition;
        public float Progress;
        public float Regression;
    }
} 