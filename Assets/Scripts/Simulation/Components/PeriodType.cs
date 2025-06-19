using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Burst;

namespace ProceduralWorld.Simulation.Components
{
    public enum PeriodType
    {
        None,
        Ancient,
        Medieval,
        Renaissance,
        Industrial,
        Modern,
        Future
    }
} 