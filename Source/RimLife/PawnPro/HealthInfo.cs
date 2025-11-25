using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace RimLife
{
    /// <summary>
    /// Represents a snapshot of a Pawn's health information.
    /// Note: This data is a snapshot and its temporal consistency is not guaranteed.
    /// Unified to a single class (previous readonly struct version removed).
    /// </summary>
    public class HealthInfo
    {
        /// <summary>
        /// Total pain level of the pawn, typically ranging from 0 to 1.
        /// </summary>
        public float SummaryPain;

        /// <summary>
        /// Total bleed rate of the pawn.
        /// </summary>
        public float SummaryBleedRate;

        /// <summary>
        /// A summary of key pawn capacities, such as 'Moving' and 'Manipulation'.
        /// Stored in a dictionary to avoid hardcoding.
        /// </summary>
        public IReadOnlyDictionary<string, float> Capacities { get; }

        /// <summary>
        /// A list of all visible injuries, diseases, and other health conditions (hediffs).
        /// </summary>
        public IReadOnlyList<HealthEntry> Injuries { get; }

        #region Factory Methods

        // Defines which capacities are considered "key" to prevent the Capacities dictionary from becoming too large.
        private static readonly PawnCapacityDef[] KeyCapacityDefs =
        [
            PawnCapacityDefOf.Moving,
            PawnCapacityDefOf.Manipulation,
            PawnCapacityDefOf.Talking,
            PawnCapacityDefOf.Consciousness,
            PawnCapacityDefOf.Sight,
            PawnCapacityDefOf.Hearing,
            PawnCapacityDefOf.Breathing
        ];

        private HealthInfo()
        {
            Capacities = new Dictionary<string, float>();
            Injuries = new List<HealthEntry>();
        }

        private HealthInfo(float summaryPain, float summaryBleedRate, IReadOnlyDictionary<string, float> capacities, IReadOnlyList<HealthEntry> injuries)
        {
            SummaryPain = summaryPain;
            SummaryBleedRate = summaryBleedRate;
            Capacities = capacities;
            Injuries = injuries;
        }

        /// <summary>
        /// Creates a HealthInfo snapshot from a Pawn. Must be called on the main thread.
        /// </summary>
        public static HealthInfo CreateFrom(Pawn p)
        {
            if (p?.health == null) return new HealthInfo();

            // Summarize pain and bleed rate (null-safe).
            var summaryPain = p.health.hediffSet?.PainTotal ?? 0f;
            var summaryBleedRate = p.health.hediffSet?.BleedRateTotal ?? 0f;

            // Get capacity levels (clamped to 0-1, as RimWorld can sometimes exceed 1).
            var capacities = new Dictionary<string, float>();
            if (p.health.capacities != null)
            {
                foreach (var def in KeyCapacityDefs)
                {
                    try
                    {
                        float level = p.health.capacities?.GetLevel(def) ?? 0f;
                        capacities[def.defName] = Mathf.Clamp01(level);
                    }
                    catch
                    {
                        // Ignore capacity exceptions.
                    }
                }
            }
            
            // Populate the list of injuries and other health conditions.
            var injuries = new List<HealthEntry>();
            var hediffs = p.health.hediffSet?.hediffs;
            if (hediffs != null)
            {
                foreach (var h in hediffs)
                {
                    if (h == null || !h.Visible) continue;
                    var entry = new HealthEntry
                    {
                        Label = h.def?.label ?? h.LabelCap,
                        Part = h.Part?.Label ?? "Whole body",
                        Severity = h.Severity,
                        IsBleeding = h.Bleeding,
                        IsPermanent = h.IsPermanent(),
                        IsInfection = h.def?.isInfection ?? false,
                        GroupTag = GetHealthGroupTag(h)
                    };
                    injuries.Add(entry);
                }
            }
            
            return new HealthInfo(summaryPain, summaryBleedRate, capacities, injuries);
        }

        /// <summary>
        /// Asynchronously creates a HealthInfo snapshot by dispatching the work to the main thread.
        /// This is an example of how to safely gather game data from a background thread.
        /// </summary>
        public static Task<HealthInfo> CreateFromAsync(Pawn p)
        {
            if (p == null) return Task.FromResult(new HealthInfo());

            return MainThreadDispatcher.EnqueueAsync(() => CreateFrom(p));
        }


        private static string GetHealthGroupTag(Hediff h)
        {
            if (h?.def == null) return "Other";
            if (h.def.isInfection) return "Disease";
            if (h.Bleeding) return "Trauma";
            if (h.IsPermanent()) return "Permanent";
            if (h.def.makesSickThought) return "Ill";
            return "Other";
        }

        #endregion
    }

    public struct HealthEntry
    {
        /// <summary>
        /// The name of the condition (e.g., "Gunshot").
        /// </summary>
        public string Label;
        /// <summary>
        /// The affected body part (e.g., "Left Arm").
        /// </summary>
        public string Part;
        /// <summary>
        /// The severity of the condition.
        /// </summary>
        public float Severity;
        /// <summary>
        /// Indicates if the condition is causing bleeding.
        /// </summary>
        public bool IsBleeding;
        /// <summary>
        /// Indicates if the condition is permanent, like a scar.
        /// </summary>
        public bool IsPermanent;
        /// <summary>
        /// Indicates if the condition is an infection or disease.
        /// </summary>
        public bool IsInfection;
        /// <summary>
        /// A tag for grouping similar conditions (e.g., "Trauma", "Disease").
        /// </summary>
        public string GroupTag;
    }
}
