using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace RimLife
{
    /// <summary>
    /// Represents a snapshot of a Pawn's backstory information.
    /// Note: This data is a snapshot and its temporal consistency is not guaranteed.
    /// </summary>
    public class BackstoryInfo
    {
        /// <summary>
        /// The childhood backstory, if available.
        /// </summary>
        public BackstoryEntry? Childhood { get; }

        /// <summary>
        /// The adulthood backstory, if available.
        /// </summary>
        public BackstoryEntry? Adulthood { get; }

        private BackstoryInfo(BackstoryEntry? childhood, BackstoryEntry? adulthood)
        {
            Childhood = childhood;
            Adulthood = adulthood;
        }

        /// <summary>
        /// Creates a BackstoryInfo snapshot from a Pawn. Must be called on the main thread.
        /// </summary>
        public static BackstoryInfo CreateFrom(Pawn p)
        {
            if (p?.story == null) return new BackstoryInfo(null, null);

            BackstoryEntry? childhood = p.story.Childhood != null ? new BackstoryEntry
            {
                Title = p.story.Childhood.title,
                Description = p.story.Childhood.description
            } : null;

            BackstoryEntry? adulthood = p.story.Adulthood != null ? new BackstoryEntry
            {
                Title = p.story.Adulthood.title,
                Description = p.story.Adulthood.description
            } : null;

            return new BackstoryInfo(childhood, adulthood);
        }
    }

    public struct BackstoryEntry
    {
        /// <summary>
        /// The title of the backstory.
        /// </summary>
        public string Title;
        /// <summary>
        /// The description of the backstory.
        /// </summary>
        public string Description;
    }
}
