using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;

namespace ProceduralWorld.Simulation.Core
{
    public struct VisualizationState : IComponentData
    {
        public bool IsDirty;
        public float LastUpdateTime;
        public float UpdateInterval;
        public float VisualizationScale;
        public float ColorIntensity;
        public float AnimationSpeed;
    }
} 