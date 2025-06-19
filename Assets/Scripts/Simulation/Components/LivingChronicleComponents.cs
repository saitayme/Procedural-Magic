using Unity.Entities;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using ProceduralWorld.Simulation.Core;

namespace ProceduralWorld.Simulation.Components
{
    // ==== LIVING CHRONICLE SYSTEM COMPONENTS ====
    
    // Core Chronicle Entry - Rich, contextual event representation
    public struct ChronicleEntry : IComponentData
    {
        public int EntryId;
        public HistoricalEventRecord OriginalEvent;
        public int Year;
        public Entity PrimaryCivilization;
        
        // Rich contextual information
        public FixedString128Bytes ContextSummary;      // Brief context summary
        public FixedString128Bytes MotivationSummary;   // Brief motivation summary
        public FixedString128Bytes ConsequencesSummary; // Brief consequences summary
        public EmotionalTone EmotionalTone;             // Emotional resonance of the event
        public float CulturalSignificance;              // How important to the civilization's culture
        
        // Narrative elements
        public float NarrativeTension;                  // Dramatic tension level
        public FixedString128Bytes CharacterDevelopment; // How this changed the civilization's character
        public int ThematicElementCount;                // Number of thematic elements
        
        // Causality tracking
        public int DirectCauseCount;                    // Number of direct causes
        public int ContributingFactorCount;             // Number of contributing factors
        public int PredictedEffectCount;                // Number of predicted effects
        
        // Literary quality
        public float DramaticWeight;                    // How dramatically significant
        public FixedString128Bytes SymbolicMeaning;     // Symbolic/metaphorical meaning
        public int HistoricalParallelCount;             // Number of historical parallels
        
        // Processing flags
        public bool IsProcessed;
        public bool NeedsFollowUp;
    }

    // Narrative Thread - Ongoing storylines that weave through history
    public struct NarrativeThread : IComponentData
    {
        public int ThreadId;
        public ThreadType Type;
        public Entity PrimaryCivilization;
        public FixedString128Bytes ThreadName;
        public FixedString512Bytes Description;
        
        // Thread progression
        public int StartYear;
        public ThreadStage CurrentStage;
        public float Tension;                           // Current dramatic tension
        public float Momentum;                          // How fast the thread is developing
        public float Significance;                      // Overall historical importance
        
        // Thread state
        public bool IsActive;
        public int LastUpdateYear;
        public int EntryCount;
        public ThematicFocus ThematicFocus;            // Primary theme of this thread
    }

    public struct NarrativeThreadData : IComponentData
    {
        public NarrativeThread ThreadData;
        public int ConnectedThreadCount;                // Number of connected threads
        public int ChronicleEntryCount;                 // Number of entries in this thread
        public float LastTensionUpdate;
        public float ThreadHealth;                      // How well the thread is developing
    }

    // Compiled Chronicle - Final readable narrative
    public struct CompiledChronicle : IComponentData
    {
        public int ChronicleId;
        public Entity CivilizationEntity;
        public FixedString128Bytes Title;
        public FixedString512Bytes Subtitle;
        public FixedString4096Bytes ChronicleText;      // The actual readable narrative
        
        // Chronicle metadata
        public int StartYear;
        public int EndYear;
        public int TotalEntries;
        public float DramaticIntensity;
        public float HistoricalSignificance;
        
        // Narrative analysis counts
        public int ThematicElementCount;
        public int CharacterArcCount;
        public int MajorEventCount;
        
        // Chronicle state
        public bool IsComplete;
    }

    // Chronicle Chapter - Organized sections of narrative
    public struct ChronicleChapter : IComponentData
    {
        public int ChapterNumber;
        public FixedString128Bytes Title;
        public FixedString512Bytes Summary;
        public int StartYear;
        public int EndYear;
        public int EntryCount;                          // Number of entries in this chapter
        public float DramaticArc;                       // How tension builds in this chapter
        public ChapterType Type;
    }

    // Causal Factor - What caused events to happen
    public struct CausalFactor : IComponentData
    {
        public FixedString128Bytes Description;
        public CausalType Type;
        public float Influence;                         // How much this factor contributed
        public Entity SourceEntity;                    // What/who was the source
        public int OriginYear;                         // When this factor began
        public bool IsOngoing;                         // Still affecting events
    }

    // Predicted Effect - What events will cause
    public struct PredictedEffect : IComponentData
    {
        public FixedString128Bytes Description;
        public EffectType Type;
        public float Probability;                       // Likelihood of this effect
        public int ExpectedYear;                       // When this effect should manifest
        public float Magnitude;                        // How significant the effect will be
        public Entity TargetEntity;                   // What will be affected
        public bool HasManifested;                     // Whether this has come to pass
    }

    // Thematic Element - Narrative themes present in events
    public struct ThematicElement : IComponentData
    {
        public ThemeType Type;
        public FixedString64Bytes Description;
        public float Strength;                         // How strongly this theme is present
        public bool IsRecurring;                       // Appears multiple times
    }

    // Character Arc - How civilizations develop over time
    public struct CharacterArc : IComponentData
    {
        public Entity CivilizationEntity;
        public FixedString128Bytes ArcName;
        public FixedString512Bytes Description;
        public CharacterArcType Type;
        public float Progress;                         // How far along the arc
        public float Intensity;                       // How dramatic the development
        public int StartYear;
        public int CurrentYear;
        public bool IsComplete;
    }

    // Major Event - Significant moments in chronicles
    public struct MajorEvent : IComponentData
    {
        public int EventId;
        public FixedString128Bytes Name;
        public FixedString512Bytes Description;
        public int Year;
        public float Significance;
        public EventImpactType ImpactType;
        public int AffectedCivilizationCount;           // Number of affected civilizations
        public bool IsClimax;                          // Is this a narrative climax
    }

    // Historical Parallel - Similar events from other civilizations
    public struct HistoricalParallel : IComponentData
    {
        public int OriginalEventId;
        public int ParallelEventId;
        public FixedString128Bytes ParallelDescription;
        public float Similarity;                       // How similar the events are
        public Entity ParallelCivilization;
        public int YearDifference;
        public ParallelType Type;
    }

    // ==== ENUMS ====
    
    public enum EmotionalTone : byte
    {
        Triumphant,
        Tragic,
        Hopeful,
        Grim,
        Peaceful,
        Tense,
        Mysterious,
        Romantic,
        Heroic,
        Melancholic,
        Neutral
    }

    public enum ThreadType : byte
    {
        General,
        PoliticalIntrigue,
        ReligiousMovement,
        Dynasty,
        TechnologicalRevolution,
        CulturalRenaissance,
        MilitaryConquest,
        EconomicTransformation,
        SocialUpheaval,
        DiplomaticSaga,
        PersonalStory,
        EnvironmentalChallenge
    }

    public enum ThreadStage : byte
    {
        Genesis,        // Thread beginning
        Development,    // Building complexity
        Complication,   // Obstacles and challenges
        Crisis,         // Major turning point
        Climax,         // Peak moment
        Resolution,     // Conclusion
        Legacy          // Lasting effects
    }

    public enum ThematicFocus : byte
    {
        Power,
        Love,
        Betrayal,
        Redemption,
        Sacrifice,
        Ambition,
        Honor,
        Wisdom,
        Corruption,
        Justice,
        Survival,
        Growth,
        Decline,
        Transformation
    }

    public enum ChapterType : byte
    {
        Introduction,
        RisingAction,
        Climax,
        FallingAction,
        Resolution,
        Epilogue,
        Interlude
    }

    public enum CausalType : byte
    {
        Environmental,  // Natural factors
        Political,      // Government/power decisions
        Economic,       // Resource/trade factors
        Social,         // Cultural/population factors
        Military,       // Warfare/conflict factors
        Religious,      // Spiritual/belief factors
        Technological,  // Innovation/knowledge factors
        Personal,       // Individual leader decisions
        Random,         // Chance/unpredictable factors
        Cyclical        // Recurring patterns
    }

    public enum EffectType : byte
    {
        Immediate,      // Happens right away
        ShortTerm,      // Within a few years
        LongTerm,       // Decades later
        Generational,   // Affects future generations
        Permanent,      // Irreversible change
        Cyclical,       // Creates recurring pattern
        Cascading       // Causes chain reaction
    }

    public enum ThemeType : byte
    {
        HeroicJourney,
        FallFromGrace,
        Redemption,
        LoveConquersAll,
        PowerCorrupts,
        UnityVsDivision,
        TraditionVsProgress,
        IndividualVsSociety,
        OrderVsChaos,
        FaithVsDoubt,
        WisdomVsIgnorance,
        CourageVsFear,
        HopeVsDespair,
        JusticeVsInjustice
    }

    public enum CharacterArcType : byte
    {
        HeroicRise,
        TragicFall,
        RedemptionStory,
        ComingOfAge,
        CorruptionArc,
        TransformationArc,
        CyclicalJourney,
        MartyrdomArc,
        EnlightenmentPath,
        DeclineAndFall
    }

    public enum EventImpactType : byte
    {
        WorldChanging,
        RegionallySignificant,
        CulturallyImportant,
        PersonallyMeaningful,
        SymbolicallyPowerful,
        CausallyImportant,
        ThematicallyRelevant
    }

    public enum ParallelType : byte
    {
        StructuralSimilarity,   // Similar sequence of events
        ThematicEcho,          // Similar themes/meanings
        CausalPattern,         // Similar cause-effect relationships
        CharacterParallel,     // Similar character development
        HistoricalRhyme        // History rhyming with itself
    }

    // ==== BUFFER COMPONENTS ====
    
    [InternalBufferCapacity(16)]
    public struct ChronicleEntryBuffer : IBufferElementData
    {
        public int EntryId;
        public float RelevanceScore;
    }

    [InternalBufferCapacity(8)]
    public struct NarrativeThreadBuffer : IBufferElementData
    {
        public int ThreadId;
        public float ParticipationLevel;
        public ThreadRole Role;
    }

    [InternalBufferCapacity(4)]
    public struct CompiledChronicleBuffer : IBufferElementData
    {
        public int ChronicleId;
        public float QualityScore;
        public bool IsPublished;
    }

    [InternalBufferCapacity(12)]
    public struct CausalChainBuffer : IBufferElementData
    {
        public int CauseEventId;
        public int EffectEventId;
        public float CausalStrength;
        public CausalType CausalType;
    }

    public enum ThreadRole : byte
    {
        Protagonist,
        Antagonist,
        SupportingCharacter,
        Catalyst,
        Victim,
        Beneficiary,
        Observer,
        Narrator
    }

    // ==== CHRONICLE ANALYSIS COMPONENTS ====
    
    // Narrative Intelligence - Analyzes and improves storytelling
    public struct NarrativeIntelligenceData : IComponentData
    {
        public float StoryQuality;              // Overall narrative quality score
        public float DramaticPacing;            // How well dramatic tension is managed
        public float CausalCoherence;           // How well cause-effect relationships work
        public float ThematicConsistency;       // How consistent themes are
        public float CharacterDevelopment;      // How well characters develop
        public float HistoricalAccuracy;        // How well it reflects actual events
        public float ReadabilityScore;          // How engaging it is to read
        public float EmotionalResonance;        // How emotionally compelling
    }

    // Chronicle Statistics - Metrics about the chronicle system
    public struct ChronicleStatistics : IComponentData
    {
        public int TotalChronicleEntries;
        public int ActiveNarrativeThreads;
        public int CompletedChronicles;
        public float AverageChronicleQuality;
        public float AverageDramaticIntensity;
        public int TotalCausalConnections;
        public int ThematicPatterns;
        public float OverallSystemHealth;
    }
} 