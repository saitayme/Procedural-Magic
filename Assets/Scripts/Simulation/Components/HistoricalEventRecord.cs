using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;
using ProceduralWorld.Simulation.Core;

namespace ProceduralWorld.Simulation.Components
{
    [BurstCompile]
    public struct HistoricalEventRecord : IComponentData
    {
        public FixedString128Bytes Title;
        public FixedString512Bytes Description;
        public int Year;
        public EventType Type;
        public EventCategory Category;
        public float3 Location;
        public float Significance;
        public Entity SourceEntityId;
        public float Size;
        public Entity FigureId;
        public Entity CivilizationId;
        public NativeList<Entity> RelatedFigures;
        public NativeList<Entity> RelatedCivilizations;
        public int Id;

        // For backward compatibility
        public FixedString128Bytes Name
        {
            get => Title;
            set => Title = value;
        }
    }
} 