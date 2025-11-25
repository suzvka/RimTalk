using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using UnityEngine; // for Mathf

namespace RimLife
{
    /// <summary>
    /// Defines the broad category of a Pawn.
    /// </summary>
    public enum PawnType
    {
        Character,
        Animal,
        Mechanoid,
        Insect,
        Other
    }

    /// <summary>
    /// Represents the relationship of a Pawn's faction to the player's faction.
    /// </summary>
    public enum PawnRelation
    {
        OurParty, // Member of the player's faction
        Ally,
        Neutral,
        Enemy,
        Other
    }

    /// <summary>
    /// Provides a lightweight proxy for a Pawn, with lazy-loaded modules for detailed information.
    /// Creation is cheap; expensive calculations are deferred until specific properties (e.g., .Perspective) are accessed.
    /// Note: This class is a data snapshot and does not update automatically. It must be created and accessed on the main game thread.
    /// The temporal consistency of the data is not strictly guaranteed; it is suitable for descriptive or narrative purposes, not for systems requiring real-time validation.
    /// </summary>
    public class PawnPro
    {
        // The original Pawn reference, used for on-demand data extraction.
        private readonly Pawn _sourcePawn;

        // --- 1. Basic Metadata ---
        public string ID { get; }
        public string Name { get; }
        public string FullName { get; }
        public string DefName { get; }
        public string FactionLabel { get; }
        public float AgeBiologicalYears { get; }
        public string Gender { get; }
        public PawnType PawnType { get; }

        public bool IsDead => _sourcePawn.Dead;
        public bool IsDowned => _sourcePawn.Downed;
        // Null-safe check for consciousness.
        public bool IsAwake => _sourcePawn.jobs?.curDriver?.asleep == false;

        // --- Constructor ---
        public PawnPro(Pawn pawn)
        {
            if (pawn == null) throw new ArgumentNullException(nameof(pawn));
            _sourcePawn = pawn;

            // Null-safe initialization with mechanoid / animal fallbacks.
            ID = pawn.ThingID;
            Name = pawn.Name?.ToStringShort ?? pawn.LabelShortCap ?? pawn.LabelShort ?? "?";
            FullName = pawn.Name?.ToStringFull ?? pawn.LabelCap ?? Name;
            DefName = pawn.def?.defName ?? "UnknownDef";
            FactionLabel = pawn.Faction?.Name ?? "Unknown";
            AgeBiologicalYears = pawn.ageTracker?.AgeBiologicalYearsFloat ??0f;
            Gender = pawn.gender.ToString();
            PawnType = GetPawnType(pawn);
        }

        // --- 2. Lazy-Loaded Modules ---

        private HealthInfo _health;
        public HealthInfo Health => _health ??= HealthInfo.CreateFrom(_sourcePawn);

        private NeedsInfo _needs;
        public NeedsInfo Needs => _needs ??= NeedsInfo.CreateFrom(_sourcePawn);

        private MoodInfo _mood;
        // Cached using the null-coalescing assignment operator.
        public MoodInfo Mood => _mood ??= (PawnType == PawnType.Character ? MoodInfo.CreateFrom(_sourcePawn) : null);

        private SkillsInfo _skills;
        public SkillsInfo Skills => _skills ??= SkillsInfo.CreateFrom(_sourcePawn);

        private ActivityInfo _activity;
        public ActivityInfo Activity => _activity ??= ActivityInfo.CreateFrom(_sourcePawn);

        private PerspectiveInfo _perspective;
        public PerspectiveInfo Perspective => _perspective ??= PerspectiveInfo.CreateFrom(_sourcePawn);

        private GearInfo _gear;
        public GearInfo Gear => _gear ??= GearInfo.CreateFrom(_sourcePawn);

        private BackstoryInfo _backstory;
        public BackstoryInfo Backstory => _backstory ??= BackstoryInfo.CreateFrom(_sourcePawn);

        // --- Helper Methods ---
        private static PawnType GetPawnType(Pawn p)
        {
            if (p.RaceProps.Humanlike) return PawnType.Character;
            if (p.RaceProps.Animal) return PawnType.Animal;
            if (p.RaceProps.IsMechanoid) return PawnType.Mechanoid;
            if (p.RaceProps.Insect) return PawnType.Insect;
            return PawnType.Other;
        }
    }
}
