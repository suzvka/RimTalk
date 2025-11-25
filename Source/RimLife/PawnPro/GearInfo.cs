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
    /// Represents a snapshot of a Pawn's gear and inventory information.
    /// Note: This data is a snapshot and its temporal consistency is not guaranteed.
    /// </summary>
    public class GearInfo
    {
        /// <summary>
        /// A list of currently worn apparel and equipment.
        /// </summary>
        public IReadOnlyList<GearItem> WornGear { get; }

        /// <summary>
        /// A list of items in the pawn's inventory.
        /// </summary>
        public IReadOnlyList<GearItem> Inventory { get; }

        private GearInfo(IReadOnlyList<GearItem> worn, IReadOnlyList<GearItem> inventory)
        {
            WornGear = worn;
            Inventory = inventory;
        }

        /// <summary>
        /// Creates a GearInfo snapshot from a Pawn. Must be called on the main thread.
        /// </summary>
        public static GearInfo CreateFrom(Pawn p)
        {
            if (p == null) return new GearInfo(new List<GearItem>(), new List<GearItem>());

            var worn = p.apparel?.WornApparel.Select(CreateGearItem).ToList() ?? new List<GearItem>();
            var inventory = p.inventory?.innerContainer.Select(CreateGearItem).ToList() ?? new List<GearItem>();

            return new GearInfo(worn, inventory);
        }

        private static GearItem CreateGearItem(Thing thing)
        {
            return new GearItem
            {
                Name = thing.LabelCap,
                Quality = thing.TryGetQuality(out var qc) ? qc.ToString() : "Normal",
                Durability = thing.def.useHitPoints ? (float)thing.HitPoints / thing.MaxHitPoints : 1f,
                Count = thing.stackCount
            };
        }
    }

    public struct GearItem
    {
        /// <summary>
        /// The name of the gear item.
        /// </summary>
        public string Name;
        /// <summary>
        /// The quality of the gear item (e.g., "Awful", "Normal", "Excellent").
        /// </summary>
        public string Quality;
        /// <summary>
        /// The durability of the gear item, ranging from 0 to 1.
        /// </summary>
        public float Durability;
        /// <summary>
        /// The stack count of the gear item.
        /// </summary>
        public int Count;
    }
}
