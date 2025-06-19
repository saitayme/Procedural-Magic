using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;

namespace ProceduralWorld.Simulation.Core
{
    // EPIC CIVILIZATION TYPES - Determines naming style and behavior
    public enum CivilizationType
    {
        Military = 0,    // Warrior cultures focused on conquest
        Technology = 1,  // Advanced civilizations focused on innovation
        Religious = 2,   // Theocratic societies focused on faith
        Trade = 3,       // Merchant republics focused on commerce
        Cultural = 4     // Artistic societies focused on culture and learning
    }

    // EPIC HEROIC LEADER TYPES - Great figures that change history
    public enum HeroicLeaderType
    {
        GreatConqueror = 0,    // Military leaders who reshape the world through conquest
        WisePhilosopher = 1,   // Thinkers whose ideas enlighten generations
        MasterBuilder = 2,     // Architects who create wonders that defy imagination
        HolyProphet = 3,       // Religious leaders who speak with divine authority
        BrilliantInventor = 4  // Innovators whose creations revolutionize civilization
    }

    // PERSONALITY TRAITS STRUCT - For epic naming and behavior
    public struct PersonalityTraits
    {
        public float Aggressiveness;  // 0-10: How likely to start conflicts
        public float Defensiveness;   // 0-10: How much they prioritize defense
        public float Greed;          // 0-10: How much they want others' resources
        public float Paranoia;       // 0-10: How much they distrust others
        public float Ambition;       // 0-10: How much they want to expand/grow
        public float Desperation;    // 0-10: How desperate they are
        public float Hatred;         // 0-10: General hostility toward others
        public float Pride;          // 0-10: How much they value their reputation
        public float Vengefulness;   // 0-10: How much they remember and seek revenge
    }
} 