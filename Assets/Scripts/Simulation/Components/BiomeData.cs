using Unity.Entities;
using Unity.Mathematics;

namespace ProceduralWorld.Simulation.Components
{
    public struct BiomeData : IComponentData
    {
        public float MinHeight;
        public float MaxHeight;
        public float MinTemperature;
        public float MaxTemperature;
        public float MinMoisture;
        public float MaxMoisture;
        public float ResourceAbundance;
        public float Fertility;
        public float Habitability;
        public float2 Color; // RGB color for visualization
    }

    public struct BiomeTransition : IComponentData
    {
        public float TransitionSmoothness;
        public float EdgeNoise;
    }
} 