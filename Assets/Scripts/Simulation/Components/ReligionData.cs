using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;
using ProceduralWorld.Simulation.Core;
using ProceduralWorld.Simulation.Components;
using ProceduralWorld.Simulation.Utils;

namespace ProceduralWorld.Simulation.Components
{
    public enum ReligionBelief
    {
        None,
        Monotheism,
        Polytheism,
        Pantheism,
        Animism,
        Atheism
    }

    [BurstCompile]
    public struct ReligionData : IComponentData
    {
        public FixedString128Bytes Name;
        public FixedString512Bytes Description;
        public float3 Position;
        public float Size;
        public float Spread;
        public float Zeal;
        public float Piety;
        public float Faith;
        public float Devotion;
        public float Worship;
        public float Ritual;
        public float Sacrifice;
        public float Prayer;
        public float Meditation;
        public float Contemplation;
        public float Enlightenment;
        public float Salvation;
        public float Redemption;
        public float Damnation;
        public float Heaven;
        public float Hell;
        public float Purgatory;
        public float Paradise;
        public float Nirvana;
        public float Reincarnation;
        public float Karma;
        public float Dharma;
        public float Moksha;
        public float Samsara;
        public float Maya;
        public float Brahman;
        public float Atman;
        public float Purusha;
        public float Prakriti;
        public float Gunas;
        public Entity Founder;
        public Entity CurrentLeader;
        public int FollowerCount;
        public int TempleCount;
        public int HolySiteCount;
        public float3 Location;
        public ReligionBelief PrimaryBelief;
        public ReligionBelief SecondaryBelief;
        public float Influence;
        public float Stability;
        public float Growth;
        public float Decline;
        public float Conversion;
        public float Resistance;
        public float Tolerance;
        public float Intolerance;
        public float Unity;
        public float Division;
        public float Peace;
        public float Conflict;
        public float Prosperity;
        public float Poverty;
        public float Health;
        public float Disease;
        public float Education;
        public float Ignorance;
        public float Knowledge;
        public float Wisdom;
        public float Folly;
        public float Intelligence;
        public float Stupidity;
        public float Genius;
        public float Idiocy;
        public float Talent;
        public float Mediocrity;
        public float Excellence;
        public float Inferiority;
        public float Superiority;
    }
} 