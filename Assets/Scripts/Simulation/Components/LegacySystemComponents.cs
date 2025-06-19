using Unity.Entities;
using Unity.Collections;
using Unity.Burst;
using Unity.Mathematics;
using ProceduralWorld.Simulation.Core;

namespace ProceduralWorld.Simulation.Components
{
    // ==== LEGACY SYSTEM COMPONENTS ====
    // These components create deep, multi-generational storytelling
    
    [BurstCompile]
    public struct LegacyData : IComponentData
    {
        public FixedString128Bytes FamilyName;
        public FixedString512Bytes FamilyMotto;
        public Entity ParentFigure;
        public Entity CurrentHeir;
        public float ReputationScore;
        public float Honor;
        public float Infamy;
        public int GenerationCount;
        public int LivingDescendants;
        public float LegacyStrength;
        public LegacyType Type;
        public float Age; // How long this legacy has existed
        public bool IsActive;
        public float3 OriginLocation;
        public Entity FoundingCivilization;
    }

    public enum LegacyType
    {
        Noble,          // Noble houses and royal families
        Merchant,       // Trading dynasties
        Scholar,        // Academic and intellectual lineages
        Military,       // Warrior bloodlines
        Religious,      // Priestly families and holy orders
        Criminal,       // Thieves guilds and crime families
        Artisan,        // Master craftsmen and artistic traditions
        Rebel           // Revolutionary movements
    }

    [BurstCompile]
    public struct MythologyData : IComponentData
    {
        public FixedString128Bytes MythName;
        public FixedString512Bytes MythContent;
        public MythType Type;
        public Entity SourceCivilization;
        public Entity SourceReligion;
        public float Believability; // How much civilizations believe this myth
        public float Spread; // How far this myth has traveled
        public int Variations; // Number of different versions
        public float MoralWeight; // How much this myth influences behavior
        public float CulturalImpact;
        public bool IsTaboo; // Some myths become forbidden
        public float3 OriginLocation;
        public int YearCreated;
        public float Authenticity; // How "true" this myth is considered
    }

    public enum MythType
    {
        CreationMyth,     // How the world/civilization began
        HeroMyth,         // Stories of great heroes
        TragedyMyth,      // Cautionary tales of downfall
        MonsterMyth,      // Legends of creatures and beasts
        LoveMyth,         // Epic romances and relationships
        WarMyth,          // Legendary battles and conflicts
        MagicMyth,        // Stories of supernatural events
        ProphecyMyth,     // Predictions and omens
        CurseMyth,        // Tales of divine punishment
        RedemptionMyth    // Stories of salvation and hope
    }

    [BurstCompile]
    public struct CulturalMemoryData : IComponentData
    {
        public FixedString128Bytes MemoryName;
        public FixedString512Bytes MemoryDescription;
        public Entity SourceEvent; // What historical event created this memory
        public MemoryType Type;
        public float EmotionalIntensity; // How strongly this is remembered
        public float Accuracy; // How accurate the memory is vs reality
        public float Influence; // How much this affects current decisions
        public int GenerationsRemembered; // How many generations know this
        public bool IsPositive; // Whether this is a good or bad memory
        public float CulturalWeight;
        public Entity AffectedCivilization;
        public float DecayRate; // How fast this memory fades
    }

    public enum MemoryType
    {
        Triumph,          // Great victories and achievements
        Trauma,           // Terrible defeats and disasters
        Betrayal,         // Being betrayed by allies
        Sacrifice,        // Noble sacrifices made
        Discovery,        // Great discoveries or revelations
        Loss,             // Losing something precious
        Miracle,          // Unexplained positive events
        Tragedy,          // Unavoidable disasters
        Humiliation,      // Being humiliated publicly
        Revenge           // Successful revenge
    }

    [BurstCompile]
    public struct AdaptivePersonalityData : IComponentData
    {
        // Dynamic personality that changes based on experiences
        public PersonalityTraits BasePersonality;
        public PersonalityTraits CurrentPersonality;
        public PersonalityTraits TemporaryModifiers;
        
        // Experience counters that shape personality
        public int SuccessfulWars;
        public int DefensiveVictories;
        public int TradeSuccesses;
        public int Betrayals;
        public int NaturalDisasters;
        public int CulturalAchievements;
        public int ReligiousEvents;
        public int DiplomaticVictories;
        
        // Personality evolution system
        public float PersonalityFlexibility; // How much personality can change
        public float CurrentStress; // Current stress level affecting personality
        public float TraumaResistance; // Resistance to negative personality changes
        public PersonalityEvolutionStage Stage;
        
        // Memory of past personality states
        public PersonalityTraits PreviousPersonality;
        public float LastPersonalityChangeYear;
    }

    public enum PersonalityEvolutionStage
    {
        Naive,        // Young civilization, personality changes easily
        Developing,   // Forming core personality
        Mature,       // Established personality, changes slowly
        Hardened,     // Rigid personality, resistant to change
        Broken,       // Traumatized, erratic personality changes
        Enlightened   // Transcended trauma, balanced personality
    }

    [BurstCompile]
    public struct NarrativeArcData : IComponentData
    {
        public FixedString128Bytes ArcName;
        public FixedString512Bytes ArcDescription;
        public NarrativeArcType Type;
        public Entity ProtagonistCivilization;
        public Entity AntagonistCivilization;
        public NarrativeStage CurrentStage;
        public float ArcProgress; // 0-1, how far through the arc we are
        public float Tension; // Current narrative tension
        public float Stakes; // What's at risk in this arc
        public bool IsEpic; // Whether this is an epic world-changing arc
        public float ExpectedDuration; // How long this arc should last
        public float StartYear;
        public int ActorsInvolved; // Number of civilizations involved
        public float AudienceEngagement; // How compelling this story is
    }

    public enum NarrativeArcType
    {
        Rise,           // Civilization rising to power
        Fall,           // Civilization collapsing
        Redemption,     // Comeback from defeat
        Tragedy,        // Inevitable doom
        Comedy,         // Happy ending after struggles
        Romance,        // Love/alliance story
        Adventure,      // Exploration and discovery
        Thriller,       // Suspense and danger
        Mystery,        // Unknown events unfolding
        Epic            // Grand, world-changing events
    }

    public enum NarrativeStage
    {
        Setup,          // Establishing the situation
        IncitingIncident, // The event that starts the story
        RisingAction,   // Building tension and conflict
        Climax,         // Peak of the story
        FallingAction,  // Consequences of the climax
        Resolution      // Final outcome
    }

    [BurstCompile]
    public struct FolkloreGenerationData : IComponentData
    {
        public FixedString128Bytes CurrentTale;
        public FixedString512Bytes TaleContent;
        public FolkloreType Type;
        public Entity SourceCivilization;
        public float Popularity; // How much people like this tale
        public float MoralLessons; // How many lessons this tale teaches
        public float EntertainmentValue;
        public float HistoricalBasis; // How much this is based on real events
        public bool IsSpreadingToOtherCivers; // Whether other civs adopt this
        public int NumberOfVariations;
        public float CulturalRelevance;
        public FolkloreMood OverallMood;
        public float AgeOfTale; // How old this tale is
    }

    public enum FolkloreType
    {
        FairyTale,      // Magical stories with clear morals
        Legend,         // Semi-historical hero stories
        Fable,          // Animal stories with morals
        Ghost,          // Supernatural horror stories
        Trickster,      // Clever character stories
        Origin,         // How things came to be
        Cautionary,     // Warning stories
        Wisdom,         // Philosophical teachings
        Seasonal,       // Stories tied to seasons/festivals
        Sacred          // Religious or spiritual stories
    }

    public enum FolkloreMood
    {
        Hopeful,        // Optimistic, encouraging
        Dark,           // Grim, warning
        Humorous,       // Funny, lighthearted
        Tragic,         // Sad, melancholy
        Mystical,       // Mysterious, spiritual
        Heroic,         // Inspiring, brave
        Romantic,       // Love-focused
        Wise,           // Teaching-focused
        Chaotic,        // Unpredictable, wild
        Peaceful        // Calm, harmonious
    }

    // Buffer for tracking multiple myths per civilization
    public struct MythologyBuffer : IBufferElementData
    {
        public Entity MythEntity;
        public float LocalBelief; // How much this civ believes this specific myth
        public float LocalVariation; // How different their version is
    }

    // Buffer for tracking cultural memories
    public struct CulturalMemoryBuffer : IBufferElementData
    {
        public Entity MemoryEntity;
        public float PersonalRelevance; // How much this affects this specific civ
        public float GenerationalDepth; // How deep this memory goes
    }

    // Buffer for tracking active narrative arcs
    public struct NarrativeArcBuffer : IBufferElementData
    {
        public Entity ArcEntity;
        public float ParticipationLevel; // How involved this civ is in this arc
        public NarrativeRole Role; // What role this civ plays in the arc
    }

    public enum NarrativeRole
    {
        Protagonist,    // Main character
        Antagonist,     // Main opponent
        Ally,          // Supporting friend
        Rival,         // Secondary opponent
        Mentor,        // Wise guide
        Victim,        // Suffers consequences
        Catalyst,      // Triggers events
        Observer,      // Watches but doesn't participate
        Wildcard,      // Unpredictable element
        Foil           // Contrasts with protagonist
    }

    // Advanced storytelling markers
    [BurstCompile]
    public struct StorytellingMarker : IComponentData
    {
        public FixedString128Bytes MarkerName;
        public StoryMarkerType Type;
        public float Significance;
        public Entity RelatedEntity;
        public float3 Location;
        public float TimeStamp;
        public bool IsResolved; // Whether this story point has been resolved
        public float EmotionalWeight;
        public int ConnectedMarkers; // How many other markers this connects to
    }

    public enum StoryMarkerType
    {
        ChekhovsGun,    // Something that must be used later
        Foreshadowing,  // Hint at future events
        PlotTwist,      // Unexpected revelation
        Callback,       // Reference to earlier events
        Irony,          // Opposite of expectation
        Climax,         // Peak moment
        Resolution,     // Story conclusion
        Cliffhanger,    // Unresolved tension
        RedHerring,     // False lead
        MacGuffin       // Object that drives the plot
    }
} 