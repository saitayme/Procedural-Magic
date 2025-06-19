using Unity.Entities;
using ProceduralWorld.Simulation.Components;

namespace ProceduralWorld.Simulation.Components
{
    public struct HistoricalEventBuffer : IBufferElementData
    {
        public HistoricalEvent Value;
    }
} 