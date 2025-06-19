using Unity.Entities;
using Unity.Collections;
using Unity.Burst;
using Unity.Mathematics;
using ProceduralWorld.Simulation.Core;

namespace ProceduralWorld.Simulation.Components
{
    [BurstCompile]
    public struct HistoricalEventData : IComponentData
    {
        public FixedString128Bytes Name;
        public FixedString512Bytes Description;
        public int Year;
        public EventType Type;
        public EventCategory Category;
        public float Significance;
        public float Influence;
        public Entity RelatedCivilization;
        public Entity RelatedFigure;
        public float3 Location;
        public float Size;
        public Entity SourceEntityId;
        public Entity FigureId;
        public FixedString128Bytes Title;
        public Entity SourceCivilization;
        public Entity TargetCivilization;
        public int RelatedFigureCount;
        public int RelatedCivilizationCount;
    }
} 