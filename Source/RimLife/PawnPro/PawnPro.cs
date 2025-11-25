using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using UnityEngine; // for Mathf

namespace RimLife
{
    // Pawn 类型
    public enum PawnType
    {
        Character, //角色
        Animal, // 动物
        Mechanoid, //机械
        Insect, // 虫族
        Other //其它
    }

    // Pawn关系
    public enum PawnRelation
    {
        OurParty, // 自己
        Ally, //盟友
        Neutral, // 中立
        Enemy, // 敌人
        Other //其他
    }

    /// <summary>
    /// 创建开销极低。只有访问具体属性（如 .Perspective）时，才会触发昂贵的计算。
    /// 注意：必须在主线程创建和访问！
    /// </summary>
    public class PawnPro
    {
        // 原始 Pawn 引用，用于按需提取数据
        private readonly Pawn _sourcePawn;

        // ---1. 基础元数据---
        public string ID { get; }
        public string Name { get; }
        public string DefName { get; }
        public string FactionLabel { get; }
        public float AgeBio { get; }
        public string Gender { get; }
        public PawnType PawnType { get; }

        public bool IsDead => _sourcePawn.Dead;
        public bool IsDowned => _sourcePawn.Downed;
        public bool IsAwake => !_sourcePawn.jobs.curDriver.asleep;

        // --- 构造函数 ---
        public PawnPro(Pawn pawn)
        {
            if (pawn == null) throw new ArgumentNullException(nameof(pawn));
            _sourcePawn = pawn;

            //立即初始化轻量数据
            ID = pawn.ThingID;
            Name = pawn.Name.ToStringShort;
            DefName = pawn.def.defName;
            FactionLabel = pawn.Faction?.Name ?? "Unknown";
            AgeBio = pawn.ageTracker.AgeBiologicalYearsFloat;
            Gender = pawn.gender.ToString();
            PawnType = GetPawnType(pawn);
        }

        // ---2. 懒加载子模块 (Lazy Modules) ---

        private HealthInfo _health;
        public HealthInfo Health => _health ??= DataExtractor.ExtractHealth(_sourcePawn);

        private NeedsInfo _needs;
        public NeedsInfo Needs => _needs ??= DataExtractor.ExtractNeeds(_sourcePawn);

        private MoodInfo _psychology;
        public MoodInfo Psychology => _psychology ?? (PawnType == PawnType.Character ? DataExtractor.ExtractMood(_sourcePawn) : null);

        private ActivityInfo _activity;
        public ActivityInfo Activity => _activity ??= DataExtractor.ExtractActivity(_sourcePawn);

        private PerspectiveInfo _perspective;
        public PerspectiveInfo Perspective => _perspective ??= PerspectiveExtractor.Capture(_sourcePawn);

        // --- 辅助方法 ---
        private static PawnType GetPawnType(Pawn p)
        {
            if (p.RaceProps.Humanlike) return PawnType.Character;
            if (p.RaceProps.Animal) return PawnType.Animal;
            if (p.RaceProps.IsMechanoid) return PawnType.Mechanoid;
            if (p.RaceProps.Insect) return PawnType.Insect;
            return PawnType.Other;
        }

        public static class DataExtractor
        {
            //关键能力选择，避免字典过大
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

            public static HealthInfo ExtractHealth(Pawn p)
            {
                var info = new HealthInfo();
                if (p?.health == null) return info;

                // 汇总疼痛与出血
                info.SummaryPain = p.health.hediffSet.PainTotal;
                info.SummaryBleedRate = p.health.hediffSet.BleedRateTotal;

                // 能力值 (0-1 范围，RimWorld 中有可能超过1，做截断)
                foreach (var def in KeyCapacityDefs)
                {
                    try
                    {
                        float level = p.health.capacities.GetLevel(def);
                        info.Capacities[def.defName] = Mathf.Clamp01(level);
                    }
                    catch
                    {
                        // 忽略异常（少数能力在当前种族可能不存在）
                    }
                }

                //伤病列表
                foreach (var h in p.health.hediffSet.hediffs)
                {
                    if (h == null || !h.Visible) continue;
                    var entry = new HealthEntry
                    {
                        Label = h.def.label ?? h.LabelCap,
                        Part = h.Part?.Label ?? "Whole body",
                        Severity = h.Severity,
                        IsBleeding = h.Bleeding,
                        IsPermanent = h.IsPermanent(),
                        IsInfection = h.def.isInfection,
                        GroupTag = GetHealthGroupTag(h)
                    };
                    info.Injuries.Add(entry);
                }

                return info;
            }

            private static string GetHealthGroupTag(Hediff h)
            {
                if (h.def.isInfection) return "Disease";
                if (h.Bleeding) return "Trauma";
                if (h.IsPermanent()) return "Permanent";
                if (h.def.makesSickThought) return "Ill";
                return "Other";
            }

            public static NeedsInfo ExtractNeeds(Pawn p)
            {
                var info = new NeedsInfo();
                if (p?.needs == null) return info;

                foreach (var need in p.needs.AllNeeds)
                {
                    if (need == null) continue;
                    try
                    {
                        float thresholdLow =0.3f; // 简单阈值占位
                        float cur = need.CurLevelPercentage;
                        bool critical = cur < (thresholdLow *0.5f) || cur <0.15f;

                        var entry = new NeedEntry
                        {
                            DefName = need.def?.defName ?? need.LabelCap,
                            Label = need.LabelCap,
                            CurLevel = cur,
                            ThresholdLow = thresholdLow,
                            IsCritical = critical
                        };
                        info.AllNeeds.Add(entry);
                    }
                    catch
                    {
                        // 忽略单个需求的异常
                    }
                }
                return info;
            }

            public static MoodInfo ExtractMood(Pawn p)
            {
                var info = new MoodInfo();
                if (p == null || !p.RaceProps.Humanlike) return info;

                // 心情与精神状态
                info.MoodLevel = p.needs?.mood?.CurLevelPercentage ??0f;
                if (p.InMentalState)
                {
                    info.MentalStateLabel = p.MentalState?.def?.label ?? p.MentalState?.InspectLine;
                }

                // 特质
                if (p.story?.traits?.allTraits != null)
                {
                    foreach (var trait in p.story.traits.allTraits)
                    {
                        if (trait == null) continue;
                        info.Traits.Add(new TraitEntry
                        {
                            DefName = trait.def.defName,
                            Label = trait.LabelCap,
                            Degree = trait.Degree
                        });
                    }
                }

                // 活跃想法（包括记忆 + 情境）
                var allThoughts = new List<Thought>();
                try { p.needs?.mood?.thoughts?.GetAllMoodThoughts(allThoughts); } catch { }
                foreach (var t in allThoughts)
                {
                    if (t == null) continue;
                    float offset =0f;
                    try { offset = t.MoodOffset(); } catch { }

                    float durationRatio =1f;
                    if (t is Thought_Memory mem)
                    {
                        //估算剩余时间比例（安全处理）
                        int duration = mem.def.DurationTicks;
                        if (duration >0)
                        {
                            durationRatio =1f - (mem.age / (float)duration);
                            durationRatio = Mathf.Clamp01(durationRatio);
                        }
                    }

                    info.ActiveThoughts.Add(new ThoughtEntry
                    {
                        Label = t.LabelCap,
                        MoodOffset = offset,
                        DurationRatio = durationRatio
                    });
                }

                return info;
            }

            public static ActivityInfo ExtractActivity(Pawn p)
            {
                var info = new ActivityInfo();
                if (p == null || p.jobs == null) return info;

                // 姿态
                try { info.Posture = p.GetPosture().ToString(); } catch { }

                // 遍历工作队列
                foreach (var job in p.jobs.jobQueue)
                {
                    if (job?.job == null) continue;
                    try
                    {
                        info.Activities.Add(new ActivityEntry
                        {
                            JobDefName = job.job.def.defName,
                            JobReport = job.job.GetReport(p)
                        });
                    }
                    catch { }
                }

                // 添加当前工作
                if (p.CurJob != null)
                {
                    try
                    {
                        info.Activities.Insert(0, new ActivityEntry
                        {
                            JobDefName = p.CurJob.def.defName,
                            JobReport = p.CurJob.GetReport(p)
                        });
                    }
                    catch { }
                }

                return info;
            }
        }
    }
}