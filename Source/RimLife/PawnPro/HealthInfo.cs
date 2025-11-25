using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using static RimLife.PawnPro;

namespace RimLife
{
	// Represents the health information of a Pawn.
	public class HealthInfo
    {
        public float SummaryPain;          // 总疼痛 (0-1)
        public float SummaryBleedRate;     // 总出血率

        // 关键能力摘要 (Key Capacities)
        // 用字典存储，避免硬编码 "Moving", "Manipulation"
        public Dictionary<string, float> Capacities = new Dictionary<string, float>();

        // 伤病列表容器
        public List<HealthEntry> Injuries = new List<HealthEntry>();
    }

    public struct HealthEntry
    {
        public string Label;        // 伤病名 (e.g. "Gunshot")
        public string Part;         // 部位 (e.g. "Left Arm")
        public float Severity;      // 严重度 (0-1 或更高)
        public bool IsBleeding;     // 是否出血
        public bool IsPermanent;    // 是否永久/疤痕
        public bool IsInfection;    // 是否是感染/疾病类
        public string GroupTag;     // 分组标签 (用于聚合，e.g. "Trauma", "Disease")
    }
}
