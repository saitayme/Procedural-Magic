using Unity.Entities;

namespace ProceduralWorld.Simulation.Core
{
    public struct WorldData : IComponentData
    {
        public float SimulationTime;
        public float SimulationSpeed;
        public float DeltaTime;
        public bool IsPaused;
    }
} 