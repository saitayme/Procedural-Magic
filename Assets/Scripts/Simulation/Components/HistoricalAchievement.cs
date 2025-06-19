using Unity.Entities;
using Unity.Collections;

namespace ProceduralWorld.Simulation.Components
{
    public struct HistoricalAchievement : IComponentData
    {
        public FixedString128Bytes Title;
        public FixedString128Bytes Description;
        public int Year;
        public float Significance;
        public Entity RelatedFigure;
        public Entity RelatedCivilization;
        public Entity RelatedEvent;
    }
} 