using Unity.Entities;
using Unity.Collections;
using Unity.Burst;
using Unity.Mathematics;
using ProceduralWorld.Simulation.Core;

namespace ProceduralWorld.Simulation.Components
{
    [BurstCompile]
    public struct WorldHistoryData : IComponentData
    {
        public int CurrentYear;
        public float TimelineProgress;
        public float MaxTimelineProgress;
        public float EventSignificanceRate;
        public float MaxEventSignificance;
        public float EventInfluenceRate;
        public float MaxEventInfluence;
        public FixedString128Bytes WorldName;
        public float WorldAge;
        public int TotalEvents;
        public int RecordedEventCount;
        public int HistoricalFigureCount;
        public int HistoricalPeriodCount;
    }
} 