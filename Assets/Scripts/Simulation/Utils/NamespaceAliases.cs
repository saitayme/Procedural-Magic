using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using ProceduralWorld.Simulation.Core;
using ProceduralWorld.Simulation.Components;
using ProceduralWorld.Simulation.Utils;

namespace ProceduralWorld.Simulation.Utils
{
    /// <summary>
    /// Provides namespace aliases for commonly used types to reduce verbosity and resolve ambiguous references
    /// </summary>
    public static class NamespaceAliases
    {
        // Core types
        public static class Core
        {
            public static class Config { }
            public static class State { }
            public static class Stats { }
            public static class Types { }
        }

        // Component types
        public static class Components
        {
            public static class Terrain { }
            public static class Simulation { }
        }

        // System types
        public static class Systems
        {
            public static class Simulation { }
        }

        // Common aliases
        public static class Aliases
        {
            public static class Random
            {
                public static class Math { }
                public static class Engine { }
            }

            public static class Simulation
            {
                public static class Config
                {
                    public static class Components { }
                    public static class Core { }
                }

                public static class SystemGroup
                {
                    public static class Core { }
                    public static class Unity { }
                }
            }
        }
    }
} 