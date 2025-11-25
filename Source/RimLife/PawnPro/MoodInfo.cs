using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace RimLife
{
	// Collects and formats Pawn mood information.
	public class MoodInfo
    {
        public float MoodLevel;            // 当前心情 (0-1)
        public string MentalStateLabel;    // 精神崩溃状态 (null if normal)

        // 特质/性格列表
        public List<TraitEntry> Traits = new List<TraitEntry>();

        // 当前活跃的想法 (Thoughts)
        public List<ThoughtEntry> ActiveThoughts = new List<ThoughtEntry>();
    }

    public struct TraitEntry
    {
        public string DefName;      // ID (e.g. "Wimp")
        public string Label;        // 显示名
        public int Degree;          // 等级 (部分 Trait 有程度之分，如 Neurotic)
    }

    public struct ThoughtEntry
    {
        public string Label;        // 想法内容 (e.g. "Ate without table")
        public float MoodOffset;    // 带来的心情影响 (+5, -3)
        public float DurationRatio; // 剩余时间比例 (可选，用于判断是否刚发生)
    }
}
