using Unity.Entities;

namespace ProceduralWorld.Simulation.Components
{
    public struct EventProcessingState : IComponentData
    {
        public bool IsDirty;
        public bool IsProcessed;
    }
} 