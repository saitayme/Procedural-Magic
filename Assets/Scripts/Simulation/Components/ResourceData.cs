using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;
using ProceduralWorld.Simulation.Core;

namespace ProceduralWorld.Simulation.Components
{
    [BurstCompile]
    public struct ResourceData : IComponentData
    {
        public FixedString128Bytes ResourceName;
        public float BaseValue;
        public float CurrentValue;
        public float MaxValue;
        public float MinValue;
        public float GrowthRate;
        public float DecayRate;
        public float Demand;
        public float Supply;
        public float Price;
        public float Quality;
        public float Rarity;
        public float Abundance;
        public float Scarcity;
        public float Value;
        public float Utility;
        public float Importance;
        public float Priority;
        public float Weight;
        public float Volume;
        public float Durability;
        public float Stability;
        public float Purity;
        public float Efficiency;
        public float Effectiveness;
        public float Potency;
        public float Strength;
        public float Power;
        public float Energy;
        public float Force;
        public float Momentum;
        public float Velocity;
        public float Acceleration;
        public float Mass;
        public float Density;
        public float Temperature;
        public float Pressure;
        public float Humidity;
        public float Moisture;
        public float Viscosity;
        public float Elasticity;
        public float Plasticity;
        public float Brittleness;
        public float Malleability;
        public float Ductility;
        public float Conductivity;
        public float Resistivity;
        public float Permeability;
        public float Porosity;
        public float Absorption;
        public float Adsorption;
        public float Solubility;
        public float Reactivity;
        public float Toxicity;
        public float Radioactivity;
        public float Corrosiveness;
        public float Flammability;
        public float Explosiveness;
        public float Volatility;
    }
} 