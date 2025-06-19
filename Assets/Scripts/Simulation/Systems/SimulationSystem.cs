using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;
using ProceduralWorld.Simulation.Core;

namespace ProceduralWorld.Simulation.Systems
{
    [UpdateInGroup(typeof(Unity.Entities.SimulationSystemGroup))]
    public partial class SimulationSystem : SystemBase
    {
        protected override void OnCreate()
        {
            base.OnCreate();
            RequireForUpdate<SimulationConfig>();
        }

        protected override void OnUpdate()
        {
            // Base simulation logic here
        }
    }
} 