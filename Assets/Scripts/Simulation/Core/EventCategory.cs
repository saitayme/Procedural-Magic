using System;

namespace ProceduralWorld.Simulation.Core
{
    [Flags]
    public enum EventCategory : long
    {
        None = 0,
        All = ~0L,
        User = 1L << 0,
        Natural = 1L << 1,
        Social = 1L << 2,
        Political = 1L << 3,
        Economic = 1L << 4,
        Religious = 1L << 5,
        Cultural = 1L << 6,
        Technological = 1L << 7,
        Military = 1L << 8,
        Environmental = 1L << 9,
        Scientific = 1L << 10,
        Medical = 1L << 11,
        Educational = 1L << 12,
        Artistic = 1L << 13,
        Philosophical = 1L << 14,
        Legal = 1L << 15,
        Infrastructure = 1L << 16,
        Transportation = 1L << 17,
        Communication = 1L << 18,
        Agricultural = 1L << 19,
        Industrial = 1L << 20,
        Commercial = 1L << 21,
        Financial = 1L << 22,
        Diplomatic = 1L << 23,
        Territorial = 1L << 24,
        Demographic = 1L << 25,
        Epidemiological = 1L << 26,
        Climatological = 1L << 27,
        Geological = 1L << 28,
        Astronomical = 1L << 29,
        Archaeological = 1L << 30,
        Interaction = 1L << 31,
        Conflict = 1L << 32,
        Growth = 1L << 33,
        Trade = 1L << 34,
        Collapse = 1L << 35,
        Diplomacy = 1L << 36,
        Expansion = 1L << 37,
        Innovation = 1L << 38,
        Recovery = 1L << 39,
        
        // NEW DRAMATIC EVENT CATEGORIES
        Hero = 1L << 40,           // Heroic leaders and great figures
        Coalition = 1L << 41,      // Coalition wars and alliances
        HolyWar = 1L << 42,        // Religious wars and crusades
        Betrayal = 1L << 43,       // Treachery and backstabbing
        Cascade = 1L << 44,        // Cascading events and chain reactions
        Revolution = 1L << 45,     // Revolutionary changes and breakthroughs
        Escalation = 1L << 46,     // War escalation and spreading conflicts
        Disaster = 1L << 47,       // Great disasters and catastrophes
        Golden = 1L << 48,         // Golden ages and prosperity
        Spiritual = 1L << 49,      // Religious awakenings and spiritual events
        Decline = 1L << 50,        // Dark ages and decline
        Discovery = 1L << 51       // Great discoveries and breakthroughs
    }
} 